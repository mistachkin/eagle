/*
 * TraceOps.cs --
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using TracePriorityDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.TracePriority, int>;

using FormatPair = Eagle._Components.Public.AnyPair<
    Eagle._Components.Public.TraceFormatType, string>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the central implementation of the diagnostic
    /// tracing subsystem used throughout the core library.  It manages the
    /// trace enabled state, the set of allowed and disallowed trace categories,
    /// the trace priority masks, the trace message format (string, index, and
    /// flags), and the actual formatting and writing of trace messages to the
    /// configured listeners, log, and/or interpreter host.  It also tracks
    /// various statistics about trace messages that were written, dropped,
    /// filtered, or otherwise handled.
    /// </summary>
    [ObjectId("6dd365ef-005a-4d33-8042-bf5b7d17153e")]
    internal static class TraceOps
    {
        #region Private Constants
#if MONO_BUILD
#pragma warning disable 414
#endif
        //
        // NOTE: This regular expression can be used to determines if a string
        //       is considered to be a valid category.  By default, this value
        //       will not be used.  To be used, it would need to be set as the
        //       value of the "TraceCategoryRegEx" field (below).
        //
        /// <summary>
        /// The default regular expression that may be used to determine if a
        /// string is considered to be a valid trace category.  By default, this
        /// value is not used.
        /// </summary>
        private static readonly Regex DefaultTraceCategoryRegEx = RegExOps.Create(
            "^[\\.0-9A-Z_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This regular expression can be used to determines if a string
        //       is considered to be a valid method name.  By default, this
        //       value will not be used.  To be used, it would need to be set
        //       as the value of the "MethodNameRegEx" field (below).
        //
        /// <summary>
        /// The default regular expression that may be used to determine if a
        /// string is considered to be a valid method name.  By default, this
        /// value is not used.
        /// </summary>
        private static readonly Regex DefaultMethodNameRegEx = RegExOps.Create(
            "^[\\.0-9A-Z_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
#if MONO_BUILD
#pragma warning restore 414
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When set to non-null, this regular expression will be used
        //       to figure out if a category is considered valid.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When set to non-null, this regular expression is used to determine
        /// if a trace category is considered valid.
        /// </summary>
        private static Regex TraceCategoryRegEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When set to non-null, this regular expression will be used
        //       to figure out if a method name is considered valid.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When set to non-null, this regular expression is used to determine
        /// if a method name is considered valid.
        /// </summary>
        private static Regex MethodNameRegEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the (initial?) portion of the format string used to
        //       indicate that the tracing subsystem was somehow reentered.
        //       There are several ways this could happen, including via use
        //       of static initializers.
        //
        /// <summary>
        /// The (initial) portion of the format string used to indicate that the
        /// tracing subsystem was somehow reentered, for example, via static
        /// initializers.
        /// </summary>
        private const string TraceNestedIndicator = "[NESTED] ";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the (effective) format string that is used by the
        //       TraceListener class in the .NET Framework.  There are two
        //       parameter specifiers:
        //
        //       0. The trace category, if any; if this is null, only the
        //          trace message itself will be written.  It should be
        //          noted that an empty string is technically valid here.
        //
        //       1. The trace message itself.
        //
        /// <summary>
        /// The (effective) format string used by the TraceListener class in the
        /// .NET Framework, where placeholder zero is the trace category and
        /// placeholder one is the trace message itself.
        /// </summary>
        private const string TraceListenerFormat = "{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the portion of the format string used to insert the
        //       optional stack trace into the final output.
        //
        /// <summary>
        /// The portion of the format string used to insert the optional stack
        /// trace into the final trace output.
        /// </summary>
        private const string TraceStackFormat = "{9}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the portion of the format string used to insert one
        //       or more new lines into the final output.
        //
        /// <summary>
        /// The portion of the format string used to insert one or more new
        /// lines into the final trace output.
        /// </summary>
        private const string TraceNewLineFormat = "{11}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The various trace formats shared between this class and the
        //       FormatOps class.  The passed format arguments are always the
        //       same; therefore, we just omit the ones we do not need for a
        //       particular format.
        //
        /// <summary>
        /// The trace format string that includes only the message body and the
        /// trailing new line(s).
        /// </summary>
        private const string BareTraceFormat = "{10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace format string that includes the subsystem prefix, the
        /// message body, and the trailing new line(s).
        /// </summary>
        private const string MinimumTraceFormat = "{0}{10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace format string that includes the subsystem prefix, the
        /// formatted date and time, the message body, and the trailing new
        /// line(s).
        /// </summary>
        private const string MediumLowTraceFormat = "{0}{1} {10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace format string that includes the subsystem prefix, the
        /// thread identifier, the message body, the optional stack trace, and
        /// the trailing new line(s).
        /// </summary>
        private const string MediumTraceFormat = "{0}{7}: {10}" +
            TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace format string that includes the subsystem prefix, the
        /// trace priority, server name, test name, application domain,
        /// interpreter, thread, and method name, followed by the message body,
        /// the optional stack trace, and the trailing new line(s).
        /// </summary>
        private const string MediumHighTraceFormat =
            "{0}[p:{2}] [s:{3}] [x:{4}] [a:{5}] [i:{6}] [t:{7}] [m:{8}]: " +
            "{10}" + TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace format string that includes every available field: the
        /// subsystem prefix, the formatted date and time, the trace priority,
        /// server name, test name, application domain, interpreter, thread, and
        /// method name, followed by the message body, the optional stack trace,
        /// and the trailing new line(s).
        /// </summary>
        private const string MaximumTraceFormat =
            "{0}[d:{1}] [p:{2}] [s:{3}] [x:{4}] [a:{5}] [i:{6}] [t:{7}] " +
            "[m:{8}]: {10}" + TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default trace format string used when no other format has been
        /// selected.
        /// </summary>
        private const string DefaultTraceFormat = MediumTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The following replacements are made in all trace format
        //       strings used by this class:
        //
        //        {0} = Special subsystem prefix, e.g. "[NESTED] ".
        //        {1} = Message DateTime.Now, ISO-8601 formatted.
        //        {2} = Message TracePriority, hexadecimal formatted.
        //        {3} = Message server name (null when not web server).
        //        {4} = Message test name (null when not test suite).
        //        {5} = Message AppDomain.Id, decimal formatted.
        //        {6} = Message Interpreter.Id, decimal formatted.
        //        {7} = Message Thread.Id, decimal formatted.
        //        {8} = Message method name (null when not available).
        //        {9} = Message stack trace (null without special flags).
        //       {10} = Message body.
        //       {11} = Always has the value of Environment.NewLine.
        //
        /// <summary>
        /// The total number of replacement parameters used by all trace format
        /// strings in this class.
        /// </summary>
        private const int FormatParameterCount = 12;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This array must be manually kept synchronized with the
        //       values of the TraceFormatType enumeration.
        //
        /// <summary>
        /// The array of available trace format strings, indexed by the values
        /// of the <see cref="TraceFormatType" /> enumeration.
        /// </summary>
        private static readonly string[] TraceFormats = {
            DefaultTraceFormat,
            BareTraceFormat,
            MinimumTraceFormat,
            MediumLowTraceFormat,
            MediumTraceFormat,
            MediumHighTraceFormat,
            MaximumTraceFormat
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This array MUST be the same size as the full and
        //          short name arrays (below).
        //
        /// <summary>
        /// The array of trace priority values, ordered from lowest to highest;
        /// this array must remain the same size as the full and short name
        /// arrays.
        /// </summary>
        private static readonly TracePriority[] TracePriorities = {
            TracePriority.Lowest,
            TracePriority.Lower,
            TracePriority.Low,
            TracePriority.MediumLow,
            TracePriority.Medium,
            TracePriority.MediumHigh,
            TracePriority.High,
            TracePriority.Higher,
            TracePriority.Highest
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This array MUST be the same size as the flag and
        //          short name arrays (above and below).
        //
        /// <summary>
        /// The array of full (long) names corresponding to each trace priority
        /// value; this array must remain the same size as the flag and short
        /// name arrays.
        /// </summary>
        private static readonly string[] TracePriorityFullNames = {
            "Lowest",
            "Lower",
            "Low",
            "MediumLow",
            "Medium",
            "MediumHigh",
            "High",
            "Higher",
            "Highest"
        };

        /// <summary>
        /// The full (long) name used to represent the "never" trace priority.
        /// </summary>
        private static readonly string NeverTracePriorityFullName = "Never";

        /// <summary>
        /// The full (long) name used to represent the "always" trace priority.
        /// </summary>
        private static readonly string AlwaysTracePriorityFullName = "Always";

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This array MUST be the same size as the flag and
        //          full name arrays (above).
        //
        /// <summary>
        /// The array of short names corresponding to each trace priority value;
        /// this array must remain the same size as the flag and full name
        /// arrays.
        /// </summary>
        private static readonly string[] TracePriorityShortNames = {
            "L3",
            "L2",
            "L1",
            "M3",
            "M2",
            "M1",
            "H3",
            "H2",
            "H1"
        };

        /// <summary>
        /// The short name used to represent the "never" trace priority.
        /// </summary>
        private static readonly string NeverTracePriorityShortName = "N1";

        /// <summary>
        /// The short name used to represent the "always" trace priority.
        /// </summary>
        private static readonly string AlwaysTracePriorityShortName = "A1";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default values for the (overridden?) trace
        //       format string and trace format index.
        //
        /// <summary>
        /// The default value for the (overridden) trace format string.
        /// </summary>
        private const string DefaultTraceFormatString = null;

        /// <summary>
        /// The default value for the (overridden) trace format index.
        /// </summary>
        private static readonly int? DefaultTraceFormatIndex = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: What is the fallback trace format when no explicit format
        //       string -OR- format index has been set?
        //
        /// <summary>
        /// The default fallback trace format used when no explicit format
        /// string or format index has been set.
        /// </summary>
        private const string DefaultFallbackTraceFormat = MediumTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If no trace format string -OR- trace format index has been
        //       explicitly set by the user, should we use a fallback trace
        //       format?
        //
        /// <summary>
        /// The default value indicating whether a fallback trace format should
        /// be used when no explicit format string or format index has been set.
        /// </summary>
        private const bool DefaultUseFallbackTraceFormat = true; // COMPAT: Eagle beta.

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the "normal" range of trace format indexes.
        //
        /// <summary>
        /// The lowest value in the normal range of trace format indexes.
        /// </summary>
        private static readonly int MinimumTraceIndex = 2;

        /// <summary>
        /// The highest value in the normal range of trace format indexes.
        /// </summary>
        private static readonly int MaximumTraceIndex = Index.Invalid;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default (reset) values for the isTracePossible
        //       and isWritePossible static fields.
        //
        // TODO: Good default?
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default (reset) value for the isTracePossible static field.
        /// </summary>
        private static bool DefaultTracePossible = true;

        /// <summary>
        /// The default (reset) value for the isWritePossible static field.
        /// </summary>
        private static bool DefaultWritePossible = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default (initial and reset) value for the
        //       isTraceEnabledByDefault static field.
        //
        // TODO: Good default?
        //
        // HACK: These are purposely not read-only.
        //
        // HACK: This is somewhat ugly naming; however, it is accurate.
        //
        /// <summary>
        /// The default (initial and reset) value for the
        /// isTraceEnabledByDefault static field.
        /// </summary>
        private static bool DefaultTraceEnabledByDefault = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the maximum number of allowed active levels for the
        //       DebugTrace and DebugWriteTo methods.  Ideally, these would be
        //       one; however, that cannot be the case due to subtle interplay
        //       between the various subsystems.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The maximum number of allowed active levels for the DebugTrace
        /// method.
        /// </summary>
        private static int DefaultMaximumTraceLevels = 2;

        /// <summary>
        /// The maximum number of allowed active levels for the DebugWriteTo
        /// method.
        /// </summary>
        private static int DefaultMaximumWriteLevels = 2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: What is the fallback trace format when no explicit format
        //       string -OR- format index has been set?
        //
        /// <summary>
        /// The current fallback trace format used when no explicit format
        /// string or format index has been set.
        /// </summary>
        private static string FallbackTraceFormat = DefaultFallbackTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If no trace format string -OR- trace format index has been
        //       explicitly set by the user, should this class fallback to
        //       using the system default trace format?
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the fallback trace format is used when no explicit
        /// format string or format index has been set.
        /// </summary>
        private static bool UseFallbackTraceFormat = DefaultUseFallbackTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only; however, they should not
        //       need to be changed.  Instead, the associated methods in this
        //       class can be called.
        //
        /// <summary>
        /// The default set of trace categories.
        /// </summary>
        private static IntDictionary DefaultTraceCategories = null;

        /// <summary>
        /// The default trace priority value used when a method overload that
        /// lacks such a parameter is used.
        /// </summary>
        private static TracePriority DefaultTracePriority =
            TracePriority.Default;

        /// <summary>
        /// The default mask of enabled trace priorities.
        /// </summary>
        private static TracePriority DefaultTracePriorities =
            TracePriority.DefaultMask;

        /// <summary>
        /// The default mask of global trace priorities.
        /// </summary>
        private static TracePriority DefaultGlobalPriorities =
            TracePriority.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default trace priority penalty applied to eligible trace
        /// categories.
        /// </summary>
        private static int DefaultCategoryPenalty = -1;

        /// <summary>
        /// The default trace priority bonus applied to eligible trace
        /// categories.
        /// </summary>
        private static int DefaultCategoryBonus = 1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The lower-case name of the "enabled" trace category type.
        /// </summary>
        private static readonly string EnabledName =
            TraceCategoryType.Enabled.ToString().ToLowerInvariant();

        /// <summary>
        /// The lower-case name of the "disabled" trace category type.
        /// </summary>
        private static readonly string DisabledName =
            TraceCategoryType.Disabled.ToString().ToLowerInvariant();

        /// <summary>
        /// The lower-case name of the "penalty" trace category type.
        /// </summary>
        private static readonly string PenaltyName =
            TraceCategoryType.Penalty.ToString().ToLowerInvariant();

        /// <summary>
        /// The lower-case name of the "bonus" trace category type.
        /// </summary>
        private static readonly string BonusName =
            TraceCategoryType.Bonus.ToString().ToLowerInvariant();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data (MUST BE DONE PRIOR TO TOUCHING GlobalState)
        #region Synchronization Objects
        //
        // BUGFIX: This is used for synchronization inside the IsTraceEnabled
        //         method, which is used by the DebugTrace method, which is
        //         used during the initialization of the static GlobalState
        //         class; therefore, it must be initialized before anything
        //         that touches the GlobalState class.
        //
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Data
        //
        // NOTE: This field is used to keep track of the initialization state
        //       of this class.  If zero, this class it not fully initialized.
        //       If one, this class is fully initialized.  A value of greater
        //       than one indicates that a call to MaybeInitialize is pending.
        //
        /// <summary>
        /// Tracks the initialization state of this class.  Zero indicates this
        /// class is not fully initialized, one indicates it is fully
        /// initialized, and a value greater than one indicates a call to
        /// MaybeInitialize is pending.
        /// </summary>
        private static int isTraceInitialized;

        //
        // NOTE: This field helps determine what the IsTracePossible method
        //       will return.  If this field is zero, no "trace" handling of
        //       any kind will be performed, including the normal formatting
        //       and category checks, etc.
        //
        /// <summary>
        /// Helps determine what the IsTracePossible method returns.  When zero,
        /// no "trace" handling of any kind is performed.
        /// </summary>
        private static bool isTracePossible = DefaultTracePossible;

        //
        // NOTE: This field helps determine what the IsWritePossible method
        //       will return.  If this field is zero, no "write" handling of
        //       any kind will be performed, including the normal formatting
        //       and category checks, etc.
        //
        /// <summary>
        /// Helps determine what the IsWritePossible method returns.  When zero,
        /// no "write" handling of any kind is performed.
        /// </summary>
        private static bool isWritePossible = DefaultWritePossible;

        //
        // NOTE: Current number of calls to DebugTrace() that are active on
        //       this thread.  This number should always be zero or one.
        //
        /// <summary>
        /// The current number of calls to the DebugTrace method that are active
        /// on this thread; this number should always be zero or one.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static int traceLevels = 0;

        //
        // NOTE: Current number of calls to DebugWriteTo() that are active
        //       on this thread.  This number should always be zero or one.
        //
        /// <summary>
        /// The current number of calls to the DebugWriteTo method that are
        /// active on this thread; this number should always be zero or one.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static int writeLevels = 0;

#if CONSOLE
        //
        // NOTE: This field is used to temporarily store diagnostic messages
        //       related to initializing this class.  It will be reset when
        //       the messages have been written to the console.  This field
        //       must occur before the calls to CheckForTracePriorities and
        //       CheckForTracePriority (below), in order to be useful.
        //
        /// <summary>
        /// Temporarily stores diagnostic messages related to initializing this
        /// class; it is reset once the messages have been written to the
        /// console.
        /// </summary>
        private static StringBuilder initializationMessages = null;
#endif

        //
        // NOTE: These are the dictionaries of trace categories that are
        //       currently "allowed" and "disallowed".  If this dictionary
        //       is empty, all categories are considered to be "allowed";
        //       otherwise, only those present in the dictionary with a
        //       non-zero associated value are "allowed".  Any trace messages
        //       that are either not "allowed" or explicitly "disallowed"
        //       will be silently dropped.
        //
        /// <summary>
        /// The dictionary of trace categories that are currently "allowed".
        /// When empty (or null), all categories are considered to be "allowed".
        /// </summary>
        private static IntDictionary enabledTraceCategories;

        /// <summary>
        /// The dictionary of trace categories that are currently "disallowed".
        /// </summary>
        private static IntDictionary disabledTraceCategories;

        //
        // NOTE: This is the dictionary of trace categories that are eligible
        //       for a trace priority "penalty" or "bonus", respectively.
        //
        /// <summary>
        /// The dictionary of trace categories that are eligible for a trace
        /// priority "penalty".
        /// </summary>
        private static IntDictionary penaltyTraceCategories;

        /// <summary>
        /// The dictionary of trace categories that are eligible for a trace
        /// priority "bonus".
        /// </summary>
        private static IntDictionary bonusTraceCategories;

        //
        // NOTE: These fields help determine what the IsTraceEnabled method
        //       will return.  They are used to check if the specified trace
        //       priority matches this mask of enabled trace priorities.
        //
        /// <summary>
        /// The mask of currently enabled trace priorities, used by the
        /// IsTraceEnabled method.
        /// </summary>
        private static TracePriority tracePriorities;

        /// <summary>
        /// The mask of global trace priorities that always apply, used by the
        /// IsTraceEnabled method.
        /// </summary>
        private static TracePriority globalPriorities;

        //
        // NOTE: This is the default trace priority value used when a method
        //       overload that lacks such a parameter is used.
        //
        /// <summary>
        /// The default trace priority value used when a method overload that
        /// lacks such a parameter is used.
        /// </summary>
        private static TracePriority defaultTracePriority;

        //
        // NOTE: This field determines if core library tracing is enabled or
        //       disabled by default.  The value of this field is only used
        //       when initializing this subsystem and then only if both the
        //       NoTrace and Trace environment variables are not set [to
        //       anything].
        //
        /// <summary>
        /// Determines if core library tracing is enabled or disabled by
        /// default.  This value is only used when initializing this subsystem,
        /// and then only if neither the NoTrace nor the Trace environment
        /// variable is set.
        /// </summary>
        private static bool? isTraceEnabledByDefault = null;

        //
        // HACK: This is part of a hack that solves a chicken-and-egg problem
        //       with the diagnostic tracing method used by this library.  We
        //       allow tracing to be disabled via an environment variable
        //       and/or the shell command line.  Unfortunately, by the time we
        //       disable tracing, many messages will have typically already
        //       been written to the trace listeners.  To prevent this noise
        //       (that the user wishes to suppress), we internalize the check
        //       (i.e. we do it from inside the core trace method itself) and
        //       initialize this variable [once] with the result of checking
        //       the environment variable.
        //
        /// <summary>
        /// Caches whether tracing is enabled, as determined (once) by checking
        /// the relevant environment variable; this internalized check prevents
        /// trace noise the user wishes to suppress.
        /// </summary>
        private static bool? isTraceEnabled = null;

        //
        // NOTE: This is the callback to consult when performing filtering
        //       without an interpreter context or its trace filter callback.
        //
        /// <summary>
        /// The callback consulted when performing trace filtering without an
        /// interpreter context or its associated trace filter callback.
        /// </summary>
        private static TraceFilterCallback traceFilterCallback;

        //
        // NOTE: When set to non-zero, all trace messages will be redirected
        //       to the associated interpreter host, if applicable.  Caution
        //       should be taken when setting this to non-zero this because
        //       that could easily result in a deadlock, depending on which
        //       locks are held by the current thread.
        //
        /// <summary>
        /// When non-zero, all trace messages are redirected to the associated
        /// interpreter host, if applicable.  Caution should be taken because
        /// this could easily result in a deadlock.
        /// </summary>
        private static int isTraceToInterpreterHost = 0;

        //
        // NOTE: This is the number of nesting levels for writing traces to
        //       the interpreter host, if applicable.  It is used to prevent
        //       any reentrancy into the interpreter host redirection code.
        //
        /// <summary>
        /// The number of nesting levels for writing traces to the interpreter
        /// host, used to prevent reentrancy into the interpreter host
        /// redirection code.
        /// </summary>
        private static int traceToInterpreterHostLevels = 0;

        //
        // NOTE: This is the current trace format string.  Normally, this is
        //       set to null.  It can be set to any valid format string as
        //       long as out-of-bounds argument (string replacement) indexes
        //       are not used.
        //
        /// <summary>
        /// The current trace format string.  Normally null; it may be set to
        /// any valid format string provided no out-of-bounds argument
        /// (replacement) indexes are used.
        /// </summary>
        private static string traceFormatString;

        //
        // NOTE: This is the current trace format index.  Normally, this is
        //       set to null.  It can be set to any valid format index.
        //
        /// <summary>
        /// The current trace format index.  Normally null; it may be set to any
        /// valid format index.
        /// </summary>
        private static int? traceFormatIndex;

        //
        // NOTE: When this value is non-zero, the formatted DateTime (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the formatted date and time (if any) is included in
        /// the trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceDateTime;

        //
        // NOTE: When this value is non-zero, the trace priority value will be
        //       included in the trace output; otherwise, it will be replaced
        //       with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the trace priority value is included in the trace
        /// output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool tracePriority;

        //
        // NOTE: When this value is non-zero, the machine name of the server
        //       (if any) will be included in the trace output; otherwise, it
        //       will be replaced with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the machine name of the server (if any) is included
        /// in the trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceServerName;

        //
        // NOTE: When this value is non-zero, the active test name (if any)
        //       will be included in the trace output; otherwise, it will
        //       be replaced with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the active test name (if any) is included in the
        /// trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceTestName;

        //
        // NOTE: When this value is non-zero, the active application domain (if
        //       any) will be included in the trace output; otherwise, it will
        //       be replaced with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the active application domain (if any) is included in
        /// the trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceAppDomain;

        //
        // NOTE: When this value is non-zero, the active interpreter (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        /// <summary>
        /// When non-zero, the active interpreter (if any) is included in the
        /// trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceInterpreter;

        //
        // NOTE: When this value is non-zero, the active thread (if any) will
        //       be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        /// <summary>
        /// When non-zero, the active thread (if any) is included in the trace
        /// output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceThreadId;

        //
        // NOTE: When this value is non-zero, the active method name (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        /// <summary>
        /// When non-zero, the active method name (if any) is included in the
        /// trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceMethod;

        //
        // NOTE: When this value is non-zero, the complete call stack (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        /// <summary>
        /// When non-zero, the complete call stack (if any) is included in the
        /// trace output; otherwise, it is replaced with a placeholder.
        /// </summary>
        private static bool traceStack;

        //
        // NOTE: When this value is non-zero, surround all trace messages with
        //       at least one new line before and after to help make them more
        //       readable.
        //
        /// <summary>
        /// When non-zero, all trace messages are surrounded with at least one
        /// new line before and after to make them more readable.
        /// </summary>
        private static bool traceExtraNewLines;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       written due to the subsystem not being (fully?) usable.
        //
        /// <summary>
        /// The total number of trace messages that have not been written due to
        /// the subsystem not being (fully) usable.
        /// </summary>
        private static long traceImpossible = 0;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       written due to having an excluded priority and/or category.
        //
        /// <summary>
        /// The total number of trace messages that have not been written due to
        /// having an excluded priority and/or category.
        /// </summary>
        private static long traceDisabled = 0;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       written due to being too noisy, duplicates, etc.
        //
        /// <summary>
        /// The total number of trace messages that have not been written due to
        /// being too noisy, duplicates, etc.
        /// </summary>
        private static long traceTripped = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       filtered out (ever).
        //
        /// <summary>
        /// The total number of trace messages that have been filtered out
        /// (ever).
        /// </summary>
        private static long traceFiltered = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       caused an exception to be caught within the trace message
        //       output pipeline.
        //
        /// <summary>
        /// The total number of trace messages that have caused an exception to
        /// be caught within the trace message output pipeline.
        /// </summary>
        private static long traceException = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       written to the listeners (ever).
        //
        /// <summary>
        /// The total number of trace messages that have been written to the
        /// listeners (ever).
        /// </summary>
        private static long traceWritten = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       logged (ever).
        //
        /// <summary>
        /// The total number of trace messages that have been logged (ever).
        /// </summary>
        private static long traceLogged = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       dropped for any reason (ever).
        //
        /// <summary>
        /// The total number of trace messages that have been dropped for any
        /// reason (ever).
        /// </summary>
        private static long traceDropped = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       seen due to lock warnings.
        //
        /// <summary>
        /// The total number of trace messages that have been seen due to lock
        /// warnings.
        /// </summary>
        private static long traceLockWarnings = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       seen due to lock errors.
        //
        /// <summary>
        /// The total number of trace messages that have been seen due to lock
        /// errors.
        /// </summary>
        private static long traceLockErrors = 0;

        //
        // NOTE: This is the integer identifier for the thread that holds
        //       the static lock, if any.
        //
        /// <summary>
        /// The integer identifier of the thread that holds the static lock, if
        /// any.
        /// </summary>
        private static long lockThreadId = 0;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Diagnostic Methods
        /// <summary>
        /// This method returns the integer identifier of the thread that
        /// currently holds the static lock, if any.
        /// </summary>
        /// <returns>
        /// The thread identifier of the lock holder, or zero if no thread is
        /// recorded as holding the lock.
        /// </returns>
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(
                ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the current thread as the holder of the static
        /// lock, but only when the lock was actually acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock was acquired by the current thread.
        /// </param>
        private static void MaybeSomebodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the record of the current thread as the holder of
        /// the static lock, but only when the lock was actually held.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock was held by the current thread.
        /// </param>
        private static void MaybeNobodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        /// <summary>
        /// This method attempts to acquire the static lock without blocking.
        /// </summary>
        /// <param name="locked">
        /// Upon success, this is set to non-zero if the static lock was
        /// acquired by the current thread; otherwise, it is set to zero.
        /// </param>
        public static void TryLock(
            ref bool locked /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
            MaybeSomebodyHasLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the static lock if it is currently held by the
        /// current thread.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock is held by the current thread; upon
        /// return, this is set to zero once the lock has been released.
        /// </param>
        public static void ExitLock(
            ref bool locked /* in, out */
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasLock(locked);
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region State Management Methods
        //
        // HACK: This method is generally called with the static lock held;
        //       however, just in case an external caller uses it, it also
        //       attempts to obtain the lock itself.
        //
        /// <summary>
        /// This method forcibly (re)initializes the entire tracing subsystem,
        /// resetting the trace format state and then initializing the trace
        /// format, categories, priorities, and default priority.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force initialization even when the relevant state has
        /// already been set.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to use the built-in default values during initialization.
        /// </param>
        private static void ForceInitialize(
            bool force,      /* in */
            bool useDefaults /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                /* NO RESULT */
                ResetTraceFormatString();

                /* NO RESULT */
                ResetTraceFormatIndex();

                /* NO RESULT */
                ResetTraceFormatFlags();

                /* NO RESULT */
                ResetFallbackTraceFormat();

                /* NO RESULT */
                ResetUseFallbackTraceFormat();

                ///////////////////////////////////////////////////////////////

                /* IGNORED */
                InitializeTraceFormat(force, useDefaults);

                /* IGNORED */
                InitializeTraceCategories(
                    TraceStateType.CategoryTypeMask | (force ?
                        TraceStateType.Force : TraceStateType.None),
                    useDefaults);

                /* IGNORED */
                InitializeTracePriorities(force, useDefaults);

                /* IGNORED */
                InitializeTracePriority(force, useDefaults);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the tracing subsystem on the first call,
        /// setting the initial trace categories mask, trace priorities mask,
        /// and default trace priority.  Subsequent calls have no effect until a
        /// matching call to MaybeTerminate.
        /// </summary>
        private static void MaybeInitialize()
        {
            if (Interlocked.Increment(ref isTraceInitialized) == 1)
            {
                //
                // BUGFIX: For Mono (6.x?), make sure the MemberTypes and
                //         BindingFlags "lookup tables" are initialized
                //         prior to calling into the EnumOps class, where
                //         they are needed to access the TryParse methods.
                //
                ObjectOps.Initialize(false);

                //
                // NOTE: Next, initialize this subsystem by setting the
                //       initial trace categories mask, trace priorities
                //       mask, and the default trace priority.  If this
                //       initialization is not done, some trace messages
                //       may be blocked or written when they should have
                //       been written or blocked, respectively.
                //
                ForceInitialize(false, true);
            }
            else
            {
                Interlocked.Decrement(ref isTraceInitialized);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is generally called with the static lock held;
        //       however, just in case an external caller uses it, it also
        //       attempts to obtain the lock itself.
        //
        /// <summary>
        /// This method terminates the tracing subsystem when the final pending
        /// initialization is undone, clearing the trace category dictionaries
        /// and resetting the trace priority masks and default priority.
        /// </summary>
        private static void MaybeTerminate()
        {
            if (Interlocked.Decrement(ref isTraceInitialized) == 0)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (enabledTraceCategories != null)
                    {
                        enabledTraceCategories.Clear();
                        enabledTraceCategories = null;
                    }

                    if (penaltyTraceCategories != null)
                    {
                        penaltyTraceCategories.Clear();
                        penaltyTraceCategories = null;
                    }

                    if (bonusTraceCategories != null)
                    {
                        bonusTraceCategories.Clear();
                        bonusTraceCategories = null;
                    }

                    if (tracePriorities != TracePriority.None)
                        tracePriorities = TracePriority.None;

                    if (globalPriorities != TracePriority.None)
                        globalPriorities = TracePriority.None;

                    if (defaultTracePriority != TracePriority.None)
                        defaultTracePriority = TracePriority.None;
                }
            }
            else
            {
                Interlocked.Increment(ref isTraceInitialized);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method appends diagnostic information about the current state of
        /// the tracing subsystem to the specified list.  It is used by the
        /// <c>_Hosts.Default.BuildEngineInfoList</c> method.
        /// </summary>
        /// <param name="list">
        /// The list to which the trace information is appended.  This parameter
        /// may be null, in which case nothing is done.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling how much detail is included.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || isTracePossible)
                {
                    localList.Add("IsTracePossible",
                        isTracePossible.ToString());
                }

                if (empty || isWritePossible)
                {
                    localList.Add("IsWritePossible",
                        isWritePossible.ToString());
                }

                if (empty || (traceLevels != 0))
                    localList.Add("TraceLevels", traceLevels.ToString());

                if (empty || (writeLevels != 0))
                    localList.Add("WriteLevels", writeLevels.ToString());

                if (empty || (tracePriorities != TracePriority.None))
                {
                    localList.Add("TracePriorities",
                        tracePriorities.ToString());
                }

                if (empty || (globalPriorities != TracePriority.None))
                {
                    localList.Add("GlobalPriorities",
                        globalPriorities.ToString());
                }

                if (empty || (defaultTracePriority != TracePriority.None))
                {
                    localList.Add("DefaultTracePriority",
                        defaultTracePriority.ToString());
                }

                if (empty || (isTraceEnabledByDefault != null))
                {
                    localList.Add("IsTraceEnabledByDefault",
                        (isTraceEnabledByDefault != null) ?
                            isTraceEnabledByDefault.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || (isTraceEnabled != null))
                {
                    localList.Add("IsTraceEnabled",
                        (isTraceEnabled != null) ?
                            isTraceEnabled.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || (traceFilterCallback != null))
                {
                    localList.Add("TraceFilterCallback",
                        FormatOps.DelegateMethodName(
                            traceFilterCallback, false, true));
                }

                if (empty || (isTraceToInterpreterHost != 0))
                {
                    localList.Add("IsTraceToInterpreterHost",
                        isTraceToInterpreterHost.ToString());
                }

                if (empty || (traceFormatString != null))
                {
                    localList.Add("TraceFormatString",
                        FormatOps.DisplayString(traceFormatString));
                }

                if (empty || (traceFormatIndex != null))
                {
                    localList.Add("TraceFormatIndex",
                        (traceFormatIndex != null) ?
                            traceFormatIndex.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || traceDateTime)
                {
                    localList.Add("TraceDateTime",
                        traceDateTime.ToString());
                }

                if (empty || tracePriority)
                {
                    localList.Add("TracePriority",
                        tracePriority.ToString());
                }

                if (empty || traceServerName)
                {
                    localList.Add("TraceServerName",
                        traceServerName.ToString());
                }

                if (empty || traceTestName)
                {
                    localList.Add("TraceTestName",
                        traceTestName.ToString());
                }

                if (empty || traceAppDomain)
                {
                    localList.Add("TraceAppDomain",
                        traceAppDomain.ToString());
                }

                if (empty || traceInterpreter)
                {
                    localList.Add("TraceInterpreter",
                        traceInterpreter.ToString());
                }

                if (empty || traceThreadId)
                {
                    localList.Add("TraceThreadId",
                        traceThreadId.ToString());
                }

                if (empty || traceMethod)
                {
                    localList.Add("TraceMethod",
                        traceMethod.ToString());
                }

                if (empty || traceStack)
                {
                    localList.Add("TraceStack",
                        traceStack.ToString());
                }

                if (empty || (enabledTraceCategories != null))
                {
                    localList.Add("EnabledTraceCategories",
                        (enabledTraceCategories != null) ?
                            enabledTraceCategories.KeysAndValuesToString(
                                null, false) : FormatOps.DisplayNull);
                }

                if (empty || (penaltyTraceCategories != null))
                {
                    localList.Add("PenaltyTraceCategories",
                        (penaltyTraceCategories != null) ?
                            penaltyTraceCategories.KeysAndValuesToString(
                                null, false) : FormatOps.DisplayNull);
                }

                if (empty || (bonusTraceCategories != null))
                {
                    localList.Add("BonusTraceCategories",
                        (bonusTraceCategories != null) ?
                            bonusTraceCategories.KeysAndValuesToString(
                                null, false) : FormatOps.DisplayNull);
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (TraceCategoryRegEx != null))
                {
                    localList.Add("TraceCategoryRegEx",
                        (TraceCategoryRegEx != null) ?
                            TraceCategoryRegEx.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || (MethodNameRegEx != null))
                {
                    localList.Add("MethodNameRegEx",
                        (MethodNameRegEx != null) ?
                            MethodNameRegEx.ToString() :
                            FormatOps.DisplayNull);
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (DefaultMaximumTraceLevels != 0))
                {
                    localList.Add("DefaultMaximumTraceLevels(System)",
                        DefaultMaximumTraceLevels.ToString());
                }

                if (empty || (DefaultMaximumWriteLevels != 0))
                {
                    localList.Add("DefaultMaximumWriteLevels(System)",
                        DefaultMaximumWriteLevels.ToString());
                }

                if (empty || (DefaultTraceCategories != null))
                {
                    localList.Add("DefaultTraceCategories(System)",
                        (DefaultTraceCategories != null) ?
                            DefaultTraceCategories.KeysAndValuesToString(
                                null, false) : FormatOps.DisplayNull);
                }

                if (empty || (DefaultTracePriority != TracePriority.None))
                {
                    localList.Add("DefaultTracePriority(System)",
                        DefaultTracePriority.ToString());
                }

                if (empty || (DefaultTracePriorities != TracePriority.None))
                {
                    localList.Add("DefaultTracePriorities(System)",
                        DefaultTracePriorities.ToString());
                }

                if (empty || (DefaultCategoryPenalty != 0))
                {
                    localList.Add("DefaultCategoryPenalty(System)",
                        DefaultCategoryPenalty.ToString());
                }

                if (empty || (DefaultCategoryBonus != 0))
                {
                    localList.Add("DefaultCategoryBonus(System)",
                        DefaultCategoryBonus.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (traceImpossible != 0))
                {
                    localList.Add("TraceImpossible",
                        traceImpossible.ToString());
                }

                if (empty || (traceDisabled != 0))
                {
                    localList.Add("TraceDisabled",
                        traceDisabled.ToString());
                }

                if (empty || (traceTripped != 0))
                {
                    localList.Add("TraceTripped",
                        traceTripped.ToString());
                }

                if (empty || (traceFiltered != 0))
                {
                    localList.Add("TraceFiltered",
                        traceFiltered.ToString());
                }

                if (empty || (traceException != 0))
                {
                    localList.Add("TraceException",
                        traceException.ToString());
                }

                if (empty || (traceWritten != 0))
                {
                    localList.Add("TraceWritten",
                        traceWritten.ToString());
                }

                if (empty || (traceLogged != 0))
                {
                    localList.Add("TraceLogged",
                        traceLogged.ToString());
                }

                if (empty || (traceDropped != 0))
                {
                    localList.Add("TraceDropped",
                        traceDropped.ToString());
                }

                if (empty || (traceLockWarnings != 0))
                {
                    localList.Add("TraceLockWarnings",
                        traceLockWarnings.ToString());
                }

                if (empty || (traceLockErrors != 0))
                {
                    localList.Add("TraceLockErrors",
                        traceLockErrors.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Trace Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Support Methods
        /// <summary>
        /// This method queries the current status of the tracing subsystem,
        /// appending a set of name and value pairs describing it to the
        /// specified list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list to which the status information is appended; a new list is
        /// allocated when this parameter is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this is set to an error message describing why the
        /// query could not be completed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode QueryStatus(
            Interpreter interpreter, /* in: OPTIONAL */
            ref StringPairList list, /* in, out */
            ref Result error         /* out */
            )
        {
            bool isFiltered = GetTraceFilterCallback(interpreter) != null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                IEnumerable<string> categories; /* REUSED */

                if (list == null)
                    list = new StringPairList();

                list.Add("isInitialized", (Interlocked.CompareExchange(
                    ref isTraceInitialized, 0, 0) > 0).ToString());

                bool? forceToListeners = DebugOps.GetForceToListeners();

                list.Add("forceToListeners", (forceToListeners != null) ?
                    ((bool)forceToListeners).ToString() : false.ToString());

                list.Add("isEnabled", (isTraceEnabled != null) ?
                    ((bool)isTraceEnabled).ToString() : null);

                list.Add("isFiltered", isFiltered.ToString());

                list.Add("areLimitsEnabled",
                    TraceLimits.IsEnabled().ToString());

                list.Add("priority",
                    GetTracePriority().ToString());

                list.Add("priorities",
                    GetTracePriorities().ToString());

                list.Add("globalPriorities",
                    GetGlobalPriorities().ToString());

                categories = ListTraceCategories(
                    TraceCategoryType.Enabled);

                list.Add("enabledCategories", (categories != null) ?
                    categories.ToString() : null);

                categories = ListTraceCategories(
                    TraceCategoryType.Disabled);

                list.Add("disabledCategories", (categories != null) ?
                    categories.ToString() : null);

                categories = ListTraceCategories(
                    TraceCategoryType.Penalty);

                list.Add("penaltyCategories", (categories != null) ?
                    categories.ToString() : null);

                categories = ListTraceCategories(
                    TraceCategoryType.Bonus);

                list.Add("bonusCategories", (categories != null) ?
                    categories.ToString() : null);

                list.Add("formatString", GetTraceFormatString());

                int? formatIndex = GetTraceFormatIndex();

                list.Add("formatIndex", (formatIndex != null) ?
                    formatIndex.ToString() : null);

                bool traceDateTime;
                bool tracePriority;
                bool traceServerName;
                bool traceTestName;
                bool traceAppDomain;
                bool traceInterpreter;
                bool traceThreadId;
                bool traceMethod;
                bool traceStack;
                bool traceExtraNewLines;

                GetTraceFormatFlags(
                    out traceDateTime, out tracePriority,
                    out traceServerName, out traceTestName,
                    out traceAppDomain, out traceInterpreter,
                    out traceThreadId, out traceMethod,
                    out traceStack, out traceExtraNewLines);

                list.Add("formatFlags", StringList.MakeList(
                    "dateTime", traceDateTime, "priority",
                    tracePriority, "serverName", traceServerName,
                    "testName", traceTestName, "appDomain",
                    traceAppDomain, "interpreter", traceInterpreter,
                    "threadId", traceThreadId, "method", traceMethod,
                    "stack", traceStack, "extraNewLines",
                    traceExtraNewLines));

                list.Add("fullContext",
                    PolicyContext.GetForceTraceFull().ToString());
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the entire tracing subsystem back to its default
        /// state, including the trace filter callback, possible and enabled
        /// flags, limits, priorities, categories, format, and indicators.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="overrideEnvironment">
        /// Non-zero to override any values that would normally be read from
        /// environment variables.
        /// </param>
        public static void ResetStatus(
            Interpreter interpreter, /* in: OPTIONAL */
            bool overrideEnvironment /* in */
            )
        {
            /* NO RESULT */
            MaybeInitialize();

            /* NO RESULT */
            ResetTraceFilterCallback(interpreter);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                /* IGNORED */
                DebugOps.ResetForceToListeners();

                /* NO RESULT */
                ResetTracePossible();

                /* NO RESULT */
                ResetTraceEnabled();

                /* NO RESULT */
                ResetTraceFilterCallback();

                /* IGNORED */
                TraceLimits.ForceResetEnabled(overrideEnvironment);

                /* NO RESULT */
                ResetTracePriority();

                /* NO RESULT */
                ResetTracePriorities();

                /* IGNORED */
                ResetTraceCategories(TraceCategoryType.Enabled);

                /* IGNORED */
                ResetTraceCategories(TraceCategoryType.Disabled);

                /* IGNORED */
                ResetTraceCategories(TraceCategoryType.Penalty);

                /* IGNORED */
                ResetTraceCategories(TraceCategoryType.Bonus);

                /* NO RESULT */
                ResetTraceFormatString();

                /* NO RESULT */
                ResetTraceFormatIndex();

                /* NO RESULT */
                ResetTraceFormatFlags();

                /* NO RESULT */
                PolicyContext.ResetForceTraceFull();

                /* NO RESULT */
                FormatOps.ResetTraceIndicators();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forcibly enables or disables one or more aspects of the
        /// tracing subsystem, as selected by the specified state type flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="stateType">
        /// The flags selecting which aspects of the tracing subsystem to modify
        /// and how (for example, reset, enable, or disable).
        /// </param>
        /// <param name="enabled">
        /// Non-zero to enable the selected aspects; zero to disable them.
        /// </param>
        /// <returns>
        /// The flags indicating which aspects of the tracing subsystem were
        /// actually modified.
        /// </returns>
        public static TraceStateType ForceEnabledOrDisabled(
            Interpreter interpreter,  /* in: OPTIONAL */
            TraceStateType stateType, /* in */
            bool enabled              /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool force = FlagOps.HasFlags(
                    stateType, TraceStateType.Force, true);

                bool overrideEnvironment = FlagOps.HasFlags(
                    stateType, TraceStateType.OverrideEnvironment, true);

                bool verboseFlags = FlagOps.HasFlags(
                    stateType, TraceStateType.VerboseFlags, true);

                bool rawIndicators = FlagOps.HasFlags(
                    stateType, TraceStateType.RawIndicators, true);

                bool seeListeners = FlagOps.HasFlags(
                    stateType, TraceStateType.SeeListeners, true);

                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Reset, true))
                {
                    /* NO RESULT */
                    ResetStatus(interpreter, overrideEnvironment);

                    result |= TraceStateType.Reset;

                    if (overrideEnvironment)
                        result |= TraceStateType.OverrideEnvironment;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Initialized, true))
                {
                    if (enabled)
                    {
                        /* NO RESULT */
                        MaybeInitialize();
                    }
                    else
                    {
                        /* NO RESULT */
                        MaybeTerminate();
                    }

                    result |= TraceStateType.Initialized;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.ForceListeners, true))
                {
                    if (DebugOps.SetForceToListeners(enabled))
                        result |= TraceStateType.ForceListeners;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Possible, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetPossible, true))
                    {
                        /* NO RESULT */
                        ResetTracePossible();

                        result |= TraceStateType.ResetPossible;
                    }
                    else
                    {
                        /* NO RESULT */
                        SetTracePossible(enabled);
                    }

                    result |= TraceStateType.Possible;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Enabled, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetEnabled, true))
                    {
                        /* NO RESULT */
                        ResetTraceEnabled();

                        result |= TraceStateType.ResetEnabled;
                    }
                    else
                    {
                        /* NO RESULT */
                        SetTraceEnabled(enabled);
                    }

                    result |= TraceStateType.Enabled;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FilterCallback, true))
                {
                    if (enabled)
                    {
                        /* NO RESULT */
                        ResetTraceFilterCallback();

                        /* NO RESULT */
                        ResetTraceFilterCallback(interpreter);

                        result |= TraceStateType.FilterCallback;
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Limits, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetLimits, true))
                    {
                        /* IGNORED */
                        TraceLimits.ForceResetEnabled(overrideEnvironment);

                        result |= TraceStateType.ResetLimits;

                        if (overrideEnvironment)
                            result |= TraceStateType.OverrideEnvironment;
                    }
                    else
                    {
                        /* IGNORED */
                        TraceLimits.MaybeAdjustEnabled(!enabled);
                    }

                    result |= TraceStateType.Limits;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Priorities, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetPriorities, true))
                    {
                        /* NO RESULT */
                        ResetTracePriorities();

                        result |= TraceStateType.ResetPriorities;
                    }
                    else
                    {
                        //
                        // TODO: Should this really set the enabled
                        //       priorities to all possible values
                        //       here?
                        //
                        /* NO RESULT */
                        SetTracePriorities(enabled ?
                            TracePriority.HasPrioritiesMask :
                            TracePriority.None);
                    }

                    result |= TraceStateType.Priorities;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Priority, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetPriority, true))
                    {
                        /* NO RESULT */
                        ResetTracePriority();

                        result |= TraceStateType.ResetPriority;
                    }
                    else
                    {
                        //
                        // BUGBUG: Should this really set the default
                        //         priority to the highest possible
                        //         here?
                        //
                        /* NO RESULT */
                        SetTracePriority(enabled ?
                            TracePriority.Highest :
                            TracePriority.None);
                    }

                    result |= TraceStateType.Priority;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Categories, true))
                {
                    if (enabled)
                    {
                        result |= InitializeTraceCategories(stateType, false);
                    }
                    else
                    {
                        if (FlagOps.HasFlags(
                                stateType, TraceStateType.EnabledCategories,
                                true))
                        {
                            result |= ResetTraceCategories(
                                TraceCategoryType.Enabled);
                        }

                        ///////////////////////////////////////////////////////

                        if (FlagOps.HasFlags(
                                stateType, TraceStateType.DisabledCategories,
                                true))
                        {
                            result |= ResetTraceCategories(
                                TraceCategoryType.Disabled);
                        }

                        ///////////////////////////////////////////////////////

                        if (FlagOps.HasFlags(
                                stateType, TraceStateType.PenaltyCategories,
                                true))
                        {
                            result |= ResetTraceCategories(
                                TraceCategoryType.Penalty);
                        }

                        ///////////////////////////////////////////////////////

                        if (FlagOps.HasFlags(
                                stateType, TraceStateType.BonusCategories,
                                true))
                        {
                            result |= ResetTraceCategories(
                                TraceCategoryType.Bonus);
                        }
                    }

                    result |= TraceStateType.Categories;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.NullCategories, true))
                {
                    /* NO RESULT */
                    AdjustTracePriorities(
                        TracePriority.NullCategoryMask, false);

                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetNullCategories,
                            true))
                    {
                        result |= TraceStateType.ResetNullCategories;
                    }
                    else
                    {
                        if (enabled)
                        {
                            /* NO RESULT */
                            AdjustTracePriorities(
                                TracePriority.AllowNullCategory, true);
                        }
                        else
                        {
                            /* NO RESULT */
                            AdjustTracePriorities(
                                TracePriority.DenyNullCategory, true);
                        }
                    }

                    result |= TraceStateType.NullCategories;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Format, true))
                {
                    if (enabled)
                    {
                        result |= InitializeTraceFormat(force, false);
                    }
                    else
                    {
                        /* NO RESULT */
                        ResetTraceFormatString();

                        /* NO RESULT */
                        ResetTraceFormatIndex();

                        /* NO RESULT */
                        ResetTraceFormatFlags();

                        /* NO RESULT */
                        ResetFallbackTraceFormat();

                        /* NO RESULT */
                        ResetUseFallbackTraceFormat();
                    }

                    result |= TraceStateType.Format;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FormatString, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetFormatString, true))
                    {
                        /* NO RESULT */
                        ResetTraceFormatString();

                        result |= TraceStateType.ResetFormatString;
                    }
                    else
                    {
                        /* NO RESULT */
                        SetTraceFormatString(enabled ?
                            GetMaximumTraceFormat() :
                            GetMinimumTraceFormat());
                    }

                    result |= TraceStateType.FormatString;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FormatIndex, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetFormatIndex, true))
                    {
                        /* NO RESULT */
                        ResetTraceFormatIndex();

                        result |= TraceStateType.ResetFormatIndex;
                    }
                    else
                    {
                        /* NO RESULT */
                        SetTraceFormatIndex(enabled ?
                            MaximumTraceIndex :
                            MinimumTraceIndex);
                    }

                    result |= TraceStateType.FormatIndex;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FormatFlags, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetFormatFlags, true))
                    {
                        /* NO RESULT */
                        ResetTraceFormatFlags();

                        result |= TraceStateType.ResetFormatFlags;
                    }
                    else
                    {
                        /* NO RESULT */
                        EnableTraceFormatFlags(enabled, verboseFlags);
                    }

                    result |= TraceStateType.FormatFlags;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FullContext, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetFullContext, true))
                    {
                        /* NO RESULT */
                        PolicyContext.ResetForceTraceFull();

                        result |= TraceStateType.ResetFullContext;
                    }
                    else
                    {
                        /* NO RESULT */
                        PolicyContext.SetForceTraceFull(enabled);
                    }

                    result |= TraceStateType.FullContext;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.FallbackFormat, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetFallbackFormat, true))
                    {
                        /* NO RESULT */
                        ResetFallbackTraceFormat();

                        /* NO RESULT */
                        ResetUseFallbackTraceFormat();

                        result |= TraceStateType.ResetFallbackFormat;
                    }
                    else
                    {
                        /* NO RESULT */
                        SetFallbackTraceFormat(enabled);

                        /* NO RESULT */
                        SetUseFallbackTraceFormat(enabled);
                    }

                    result |= TraceStateType.FallbackFormat;
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Indicators, true))
                {
                    if (FlagOps.HasFlags(
                            stateType, TraceStateType.ResetIndicators, true))
                    {
                        /* NO RESULT */
                        FormatOps.ResetTraceIndicators();

                        result |= TraceStateType.ResetIndicators;
                    }
                    else
                    {
                        /* NO RESULT */
                        FormatOps.SetTraceIndicators(
                            enabled, rawIndicators, seeListeners);
                    }

                    result |= TraceStateType.Indicators;
                }

                ///////////////////////////////////////////////////////////////

                //
                // HACK: *SPECIAL* This enumeration value is used to mean that
                //       all internal state should be changed *if* it involves
                //       reading from environment variables.
                //
                if (FlagOps.HasFlags(
                        stateType, TraceStateType.Environment, true))
                {
                    if (enabled)
                    {
                        result |= InitializeTraceFormat(force, false);
                        result |= InitializeTraceCategories(stateType, false);
                        result |= InitializeTracePriorities(force, false);
                        result |= InitializeTracePriority(force, false);
                    }
                    else
                    {
                        TraceStateType newStateType = stateType;

                        newStateType &= ~TraceStateType.Environment;
                        newStateType |= TraceStateType.EnvironmentMask;

                        /* RECURSIVE */
                        result |= ForceEnabledOrDisabled(
                            interpreter, newStateType, enabled);
                    }

                    result |= TraceStateType.Environment;
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method inserts a name and value pair describing the specified
        /// trace state type at the beginning of the specified list, when the
        /// state type is available.
        /// </summary>
        /// <param name="stateType">
        /// The trace state type to record, if any.  This parameter may be null,
        /// in which case nothing is done.
        /// </param>
        /// <param name="enabled">
        /// When non-null, indicates whether the state type represents an
        /// enabled or disabled state, which selects the recorded name.
        /// </param>
        /// <param name="list">
        /// The list into which the name and value pair is inserted; a new list
        /// is allocated when this parameter is null.
        /// </param>
        public static void MaybeAddResultStateType(
            TraceStateType? stateType, /* in: OPTIONAL */
            bool? enabled,             /* in: OPTIONAL */
            ref StringPairList list    /* in, out */
            )
        {
            if (stateType != null)
            {
                string name;

                if (enabled != null)
                {
                    name = ((bool)enabled) ?
                        "enabledStateType" : "disabledStateType";
                }
                else
                {
                    name = "stateType";
                }

                TraceStateType value = (TraceStateType)stateType;

                if (list == null)
                    list = new StringPairList();

                list.Insert(0, new StringPair(name, value.ToString()));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClientData Support Methods
        /// <summary>
        /// This method unpacks the individual fields of the specified trace
        /// client data object into the corresponding output parameters.
        /// </summary>
        /// <param name="traceClientData">
        /// The trace client data object to unpack.
        /// </param>
        /// <param name="clientData">
        /// Upon return, the opaque client data carried by the trace client
        /// data.
        /// </param>
        /// <param name="interpreter">
        /// Upon return, the interpreter context, if any.
        /// </param>
        /// <param name="listeners">
        /// Upon return, the collection of trace listeners, if any.
        /// </param>
        /// <param name="logName">
        /// Upon return, the name of the log, if any.
        /// </param>
        /// <param name="logFileName">
        /// Upon return, the file name of the log, if any.
        /// </param>
        /// <param name="logEncoding">
        /// Upon return, the encoding to use for the log, if any.
        /// </param>
        /// <param name="logFlags">
        /// Upon return, the flags controlling log behavior, if any.
        /// </param>
        /// <param name="enabledCategories">
        /// Upon return, the trace categories to enable, if any.
        /// </param>
        /// <param name="disabledCategories">
        /// Upon return, the trace categories to disable, if any.
        /// </param>
        /// <param name="penaltyCategories">
        /// Upon return, the trace categories eligible for a priority penalty, if
        /// any.
        /// </param>
        /// <param name="bonusCategories">
        /// Upon return, the trace categories eligible for a priority bonus, if
        /// any.
        /// </param>
        /// <param name="stateType">
        /// Upon return, the trace state type flags to apply.
        /// </param>
        /// <param name="priorities">
        /// Upon return, the trace priorities to apply, if any.
        /// </param>
        /// <param name="formatString">
        /// Upon return, the trace format string to apply, if any.
        /// </param>
        /// <param name="formatIndex">
        /// Upon return, the trace format index to apply, if any.
        /// </param>
        /// <param name="forceEnabled">
        /// Upon return, the value indicating whether tracing should be forcibly
        /// enabled or disabled, if any.
        /// </param>
        /// <param name="resetSystem">
        /// Upon return, non-zero if the tracing subsystem should be reset.
        /// </param>
        /// <param name="resetListeners">
        /// Upon return, non-zero if the trace listeners should be reset.
        /// </param>
        /// <param name="trace">
        /// Upon return, non-zero if tracing should be enabled.
        /// </param>
        /// <param name="debug">
        /// Upon return, non-zero if debugging should be enabled.
        /// </param>
        /// <param name="verbose">
        /// Upon return, non-zero if verbose output is requested.
        /// </param>
        /// <param name="useDefault">
        /// Upon return, non-zero if the default trace listener should be used.
        /// </param>
        /// <param name="useConsole">
        /// Upon return, non-zero if the console trace listener should be used.
        /// </param>
        /// <param name="useNative">
        /// Upon return, non-zero if the native trace listener should be used.
        /// </param>
        /// <param name="rawLogFile">
        /// Upon return, non-zero if the log file should be written without any
        /// added formatting.
        /// </param>
        /// <param name="useStatusForm">
        /// Upon return, non-zero if the status form should be used.
        /// </param>
        /// <param name="useIndicators">
        /// Upon return, the value indicating whether trace indicators should be
        /// used, if any.
        /// </param>
        /// <param name="rawIndicators">
        /// Upon return, non-zero if trace indicators should be emitted without
        /// any added formatting.
        /// </param>
        /// <param name="seeListeners">
        /// Upon return, non-zero if trace indicators should also be visible to
        /// the trace listeners.
        /// </param>
        private static void UnpackClientData(
            TraceClientData traceClientData,
            out IClientData clientData,
            out Interpreter interpreter,
            out TraceListenerCollection listeners,
            out string logName,
            out string logFileName,
            out Encoding logEncoding,
            out LogFlags? logFlags,
            out IEnumerable<string> enabledCategories,
            out IEnumerable<string> disabledCategories,
            out IEnumerable<string> penaltyCategories,
            out IEnumerable<string> bonusCategories,
            out TraceStateType stateType,
            out TracePriority? priorities,
            out string formatString,
            out int? formatIndex,
            out bool? forceEnabled,
            out bool resetSystem,
            out bool resetListeners,
            out bool trace,
            out bool debug,
            out bool verbose,
            out bool useDefault,
            out bool useConsole,
            out bool useNative,
            out bool rawLogFile,
            out bool useStatusForm,
            out bool? useIndicators,
            out bool rawIndicators,
            out bool seeListeners
            )
        {
            clientData = traceClientData.ClientData;
            interpreter = traceClientData.Interpreter;
            listeners = traceClientData.Listeners;
            logName = traceClientData.LogName;
            logFileName = traceClientData.LogFileName;
            logEncoding = traceClientData.LogEncoding;
            logFlags = traceClientData.LogFlags;
            enabledCategories = traceClientData.EnabledCategories;
            disabledCategories = traceClientData.DisabledCategories;
            penaltyCategories = traceClientData.PenaltyCategories;
            bonusCategories = traceClientData.BonusCategories;
            stateType = traceClientData.StateType;
            priorities = traceClientData.Priorities;
            formatString = traceClientData.FormatString;
            formatIndex = traceClientData.FormatIndex;
            forceEnabled = traceClientData.ForceEnabled;
            resetSystem = traceClientData.ResetSystem;
            resetListeners = traceClientData.ResetListeners;
            trace = traceClientData.Trace;
            debug = traceClientData.Debug;
            verbose = traceClientData.Verbose;
            useDefault = traceClientData.UseDefault;
            useConsole = traceClientData.UseConsole;
            useNative = traceClientData.UseNative;
            rawLogFile = traceClientData.RawLogFile;
            useStatusForm = traceClientData.UseStatusForm;
            useIndicators = traceClientData.UseIndicators;
            rawIndicators = traceClientData.RawIndicators;
            seeListeners = traceClientData.SeeListeners;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes the specified trace client data, applying its
        /// requested changes to the tracing subsystem (for example, resetting
        /// state, configuring listeners, categories, priorities, and format).
        /// </summary>
        /// <param name="traceClientData">
        /// The trace client data describing the requested changes.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this is set to an error message describing the
        /// problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ProcessClientData(
            TraceClientData traceClientData,
            ref Result result
            )
        {
            if (traceClientData == null)
            {
                result = "invalid trace client data";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            IClientData clientData;
            Interpreter interpreter;
            TraceListenerCollection listeners;
            string logName;
            string logFileName;
            Encoding logEncoding;
            LogFlags? logFlags;
            IEnumerable<string> enabledCategories;
            IEnumerable<string> disabledCategories;
            IEnumerable<string> penaltyCategories;
            IEnumerable<string> bonusCategories;
            TraceStateType stateType;
            TracePriority? priorities;
            string formatString;
            int? formatIndex;
            bool? forceEnabled;
            bool resetSystem;
            bool resetListeners;
            bool trace;
            bool debug;
            bool verbose;
            bool useDefault;
            bool useConsole;
            bool useNative;
            bool rawLogFile;
            bool useStatusForm;
            bool? useIndicators;
            bool rawIndicators;
            bool seeListeners;

            UnpackClientData(
                traceClientData, out clientData, out interpreter,
                out listeners, out logName, out logFileName,
                out logEncoding, out logFlags, out enabledCategories,
                out disabledCategories, out penaltyCategories,
                out bonusCategories, out stateType, out priorities,
                out formatString, out formatIndex, out forceEnabled,
                out resetSystem, out resetListeners, out trace,
                out debug, out verbose, out useDefault,
                out useConsole, out useNative, out rawLogFile,
                out useStatusForm, out useIndicators, out rawIndicators,
                out seeListeners);

            ///////////////////////////////////////////////////////////////////

            bool overrideEnvironment = FlagOps.HasFlags(
                stateType, TraceStateType.OverrideEnvironment, true);

            ///////////////////////////////////////////////////////////////////

            if (resetSystem)
            {
                /* NO RESULT */
                ResetStatus(interpreter, overrideEnvironment);

                traceClientData.AddResult("ResetStatus");
                traceClientData.AddResult(FormatOps.DisplayNoResult);
            }

            ///////////////////////////////////////////////////////////////////

            if (forceEnabled != null)
            {
                TraceStateType? resultStateType = ForceEnabledOrDisabled(
                    interpreter, stateType, (bool)forceEnabled);

                traceClientData.AddResult(String.Format(
                    "ForceEnabledOrDisabled({0})", FormatOps.WrapOrNull(
                    forceEnabled)));

                traceClientData.AddResult(resultStateType);
            }

            ///////////////////////////////////////////////////////////////////

            if (priorities != null)
            {
                /* NO RESULT */
                SetTracePriorities((TracePriority)priorities);

                traceClientData.AddResult("SetTracePriorities");
                traceClientData.AddResult(priorities);
            }

            ///////////////////////////////////////////////////////////////////

            if (formatString != null)
            {
                /* NO RESULT */
                SetTraceFormatString(formatString);

                traceClientData.AddResult("SetTraceFormatString");
                traceClientData.AddResult(formatString);
            }

            ///////////////////////////////////////////////////////////////////

            if (formatIndex != null)
            {
                /* NO RESULT */
                SetTraceFormatIndex((int)formatIndex);

                traceClientData.AddResult("SetTraceFormatIndex");
                traceClientData.AddResult(formatIndex);
            }

            ///////////////////////////////////////////////////////////////////

            if (enabledCategories != null)
            {
                /* NO RESULT */
                SetTraceCategories(
                    TraceCategoryType.Enabled, enabledCategories, 1);

                traceClientData.AddResult("SetTraceCategories");
                traceClientData.AddResult(TraceCategoryType.Enabled);
            }

            ///////////////////////////////////////////////////////////////////

            if (disabledCategories != null)
            {
                /* NO RESULT */
                SetTraceCategories(
                    TraceCategoryType.Disabled, disabledCategories, 1);

                traceClientData.AddResult("SetTraceCategories");
                traceClientData.AddResult(TraceCategoryType.Disabled);
            }

            ///////////////////////////////////////////////////////////////////

            if (penaltyCategories != null)
            {
                /* NO RESULT */
                SetTraceCategories(
                    TraceCategoryType.Penalty, penaltyCategories, 1);

                traceClientData.AddResult("SetTraceCategories");
                traceClientData.AddResult(TraceCategoryType.Penalty);
            }

            ///////////////////////////////////////////////////////////////////

            if (bonusCategories != null)
            {
                /* NO RESULT */
                SetTraceCategories(
                    TraceCategoryType.Bonus, bonusCategories, 1);

                traceClientData.AddResult("SetTraceCategories");
                traceClientData.AddResult(TraceCategoryType.Bonus);
            }

            ///////////////////////////////////////////////////////////////////

            TraceStateType localStateType; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            if (useIndicators != null)
            {
                /* NO RESULT */
                FormatOps.SetTraceIndicators(
                    (bool)useIndicators, rawIndicators, seeListeners);

                localStateType = (bool)useIndicators ?
                    TraceStateType.Indicators : TraceStateType.None;

                if (rawIndicators)
                    localStateType |= TraceStateType.RawIndicators;

                if (seeListeners)
                    localStateType |= TraceStateType.SeeListeners;

                traceClientData.AddResult("SetTraceIndicators");
                traceClientData.AddResult(localStateType);
            }

            ///////////////////////////////////////////////////////////////////

            ReturnCode code; /* REUSED */
            Result localResult; /* REUSED */
            int errorCount = 0;

            ///////////////////////////////////////////////////////////////////

            if (resetListeners)
            {
                localResult = null;

                code = DebugOps.ClearTraceListeners(
                    listeners, debug, useConsole, verbose,
                    ref localResult);

                traceClientData.AddResult("ClearTraceListeners");

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            if (useDefault)
            {
                localResult = null;

                code = DebugOps.AddTraceListener(
                    listeners, TraceListenerType.Default,
                    clientData, resetListeners, ref localResult);

                traceClientData.AddResult(String.Format(
                    "AddTraceListener({0})", FormatOps.WrapOrNull(
                    TraceListenerType.Default)));

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            if (useConsole)
            {
                localResult = null;

                code = DebugOps.AddTraceListener(
                    listeners, TraceListenerType.Console,
                    clientData, resetListeners, ref localResult);

                traceClientData.AddResult(String.Format(
                    "AddTraceListener({0})", FormatOps.WrapOrNull(
                    TraceListenerType.Console)));

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            if (useNative)
            {
                localResult = null;

                code = DebugOps.AddTraceListener(
                    listeners, TraceListenerType.Native,
                    clientData, resetListeners, ref localResult);

                traceClientData.AddResult(String.Format(
                    "AddTraceListener({0})", FormatOps.WrapOrNull(
                    TraceListenerType.Native)));

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            if (logFileName != null)
            {
#if TEST && SHELL
                logName = ShellOps.GetTraceListenerName(logName,
                    GlobalState.GetCurrentSystemThreadId());

                localResult = null;

                code = DebugOps.SetupTraceLogFile(
                    logName, logFileName, logEncoding, logFlags, trace,
                    debug, useConsole, verbose, false, ref localResult);
#else
                localResult = "not implemented";
                code = ReturnCode.Error;
#endif

                traceClientData.AddResult("SetupTraceLogFile");

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }
            else if (rawLogFile)
            {
                localResult = null;

                code = DebugOps.AddTraceListener(
                    listeners, TraceListenerType.RawLogFile,
                    clientData, resetListeners, ref localResult);

                traceClientData.AddResult(String.Format(
                    "AddTraceListener({0})", FormatOps.WrapOrNull(
                    TraceListenerType.RawLogFile)));

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            if (useStatusForm)
            {
                localResult = null;

                code = DebugOps.AddTraceListener(
                    listeners, TraceListenerType.StatusForm,
                    clientData, resetListeners, ref localResult);

                traceClientData.AddResult(String.Format(
                    "AddTraceListener({0})", FormatOps.WrapOrNull(
                    TraceListenerType.StatusForm)));

                if (localResult != null)
                    localResult.ReturnCode = code;

                traceClientData.AddResult(localResult);

                if (code != ReturnCode.Ok)
                    errorCount++;
            }

            ///////////////////////////////////////////////////////////////////

            result = traceClientData.Results;
            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
#if CONSOLE
        /// <summary>
        /// This method appends a diagnostic initialization message to the
        /// pending buffer of such messages.
        /// </summary>
        /// <param name="message">
        /// The message to append.  This parameter may be null, in which case no
        /// message is appended.
        /// </param>
        private static void AppendInitializationMessage(
            string message /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (initializationMessages == null)
                {
                    initializationMessages =
                        StringBuilderFactory.CreateNoCache(); /* EXEMPT */
                }

                if (message != null)
                    initializationMessages.AppendLine(message);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the Interpreter.ProcessStartupOptions method.
        //
        /// <summary>
        /// This method writes any pending diagnostic initialization messages to
        /// the console and then clears the pending buffer.  It is used by the
        /// <c>Interpreter.ProcessStartupOptions</c> method.
        /// </summary>
        /// <param name="console">
        /// Non-zero if output to the console is permitted.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit the messages verbosely.
        /// </param>
        public static void MaybeWriteInitializationMessages(
            bool console, /* in */
            bool verbose  /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (initializationMessages != null)
                {
                    string value = initializationMessages.ToString();

                    if (value != null)
                    {
                        value = value.Trim();

                        if (!String.IsNullOrEmpty(value))
                        {
                            ConsoleOps.MaybeWritePrompt(
                                value, console, verbose);
                        }
                    }

                    initializationMessages.Length = 0;
                    initializationMessages = null;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified string consists solely
        /// of period and identifier characters, making it suitable for display
        /// without escaping.
        /// </summary>
        /// <param name="value">
        /// The string value to examine.
        /// </param>
        /// <returns>
        /// True if the string is non-empty and contains only period and
        /// identifier characters; otherwise, false.
        /// </returns>
        private static bool CanDisplayString(
            string value /* in */
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            int length = value.Length;

            for (int index = 0; index < length; index++)
            {
                char character = value[index];

                if ((character != Characters.Period) &&
                    !Parser.IsIdentifier(character))
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace category is
        /// suitable for display, using the configured trace category regular
        /// expression when one is set.
        /// </summary>
        /// <param name="category">
        /// The trace category to examine.
        /// </param>
        /// <returns>
        /// True if the category is considered valid for display; otherwise,
        /// false.
        /// </returns>
        public static bool CanDisplayCategory(
            string category /* in */
            )
        {
            if (!String.IsNullOrEmpty(category))
            {
                if (TraceCategoryRegEx != null)
                {
                    Match match = TraceCategoryRegEx.Match(category);

                    if ((match != null) && match.Success)
                        return true;
                }
                else
                {
                    return CanDisplayString(category);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified method name is suitable
        /// for display, using the configured method name regular expression
        /// when one is set.
        /// </summary>
        /// <param name="methodName">
        /// The method name to examine.
        /// </param>
        /// <returns>
        /// True if the method name is considered valid for display; otherwise,
        /// false.
        /// </returns>
        public static bool CanDisplayMethodName(
            string methodName /* in */
            )
        {
            if (!String.IsNullOrEmpty(methodName))
            {
                if (MethodNameRegEx != null)
                {
                    Match match = MethodNameRegEx.Match(methodName);

                    if ((match != null) && match.Success)
                        return true;
                }
                else
                {
                    return CanDisplayString(methodName);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified environment variable for an
        /// overridden trace format and, when present and valid, returns it as a
        /// format type and string pair.
        /// </summary>
        /// <param name="envVarName">
        /// The name of the environment variable to check.
        /// </param>
        /// <returns>
        /// A pair containing the trace format type and string when a valid
        /// override is present; otherwise, null.
        /// </returns>
        private static FormatPair CheckForTraceFormat(
            string envVarName /* in */
            )
        {
            string stringValue = CommonOps.Environment.GetVariable(
                envVarName);

            if (stringValue == null)
                return null; /* NOT OVERRIDDEN */

            TraceFormatType formatType = TraceFormatType.Unknown;
            ResultList errors = null;

            if (VerifyTraceFormat(
                    stringValue, ref formatType, ref errors))
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTraceFormat,
                    formatType));
#endif

                return new FormatPair(formatType, stringValue);
            }
            else
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTraceFormatError,
                    errors));
#endif

                return null; /* SYSTEM DEFAULT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified environment variable for a list of
        /// trace categories and, when present, returns them as a dictionary
        /// mapping each category to the specified value.
        /// </summary>
        /// <param name="envVarName">
        /// The name of the environment variable to check.
        /// </param>
        /// <param name="type">
        /// The descriptive name of the category type, used for diagnostic
        /// messages.
        /// </param>
        /// <param name="value">
        /// The value to associate with each parsed category.
        /// </param>
        /// <returns>
        /// A dictionary of the parsed trace categories when the environment
        /// variable is present; otherwise, null.
        /// </returns>
        private static IntDictionary CheckForTraceCategories(
            string envVarName, /* in */
            string type,       /* in */
            int value          /* in */
            )
        {
            string stringValue = CommonOps.Environment.GetVariable(
                envVarName);

            if (stringValue == null)
                return null; /* NOT OVERRIDDEN */

            stringValue = StringOps.NormalizeListSeparators(stringValue);

            StringList list = null;
            Result error = null;

            if (ParserOps<string>.SplitList(
                    null, stringValue, 0, _Constants.Length.Invalid,
                    true, ref list, ref error) == ReturnCode.Ok)
            {
                IntDictionary dictionary = new IntDictionary();

                if (list != null)
                {
                    foreach (string element in list)
                    {
                        if (element == null)
                            continue;

                        dictionary[element] = value;
                    }
                }

#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTraceCategories,
                    type, dictionary));
#endif

                return dictionary;
            }
            else
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTraceCategoriesError,
                    type, error));
#endif

                return null; /* SYSTEM DEFAULT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified environment variable for an
        /// overridden default trace priority and, when present and valid,
        /// returns it.
        /// </summary>
        /// <param name="envVarName">
        /// The name of the environment variable to check.
        /// </param>
        /// <returns>
        /// The overridden default trace priority when the environment variable
        /// is present and valid; otherwise, null.
        /// </returns>
        private static TracePriority? CheckForTracePriority(
            string envVarName /* in */
            )
        {
            string stringValue = CommonOps.Environment.GetVariable(
                envVarName);

            if (stringValue == null)
                return null; /* NOT OVERRIDDEN */

            object enumValue;
            Result error = null;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(TracePriority),
                DefaultTracePriority.ToString(), stringValue,
                null, true, true, true, ref error);

            if (enumValue is TracePriority)
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTracePriority,
                    enumValue));
#endif

                return (TracePriority)enumValue;
            }
            else
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTracePriorityError,
                    error));
#endif

                return null; /* SYSTEM DEFAULT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified environment variable for an
        /// overridden mask of enabled trace priorities and, when present and
        /// valid, returns it.
        /// </summary>
        /// <param name="envVarName">
        /// The name of the environment variable to check.
        /// </param>
        /// <returns>
        /// The overridden mask of enabled trace priorities when the environment
        /// variable is present and valid; otherwise, null.
        /// </returns>
        private static TracePriority? CheckForTracePriorities(
            string envVarName /* in */
            )
        {
            string stringValue = CommonOps.Environment.GetVariable(
                envVarName);

            if (stringValue == null)
                return null; /* NOT OVERRIDDEN */

            object enumValue;
            Result error = null;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(TracePriority),
                DefaultTracePriorities.ToString(), stringValue,
                null, true, true, true, ref error);

            if (enumValue is TracePriority)
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTracePriorities,
                    enumValue));
#endif

                return (TracePriority)enumValue;
            }
            else
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultTracePrioritiesError,
                    error));
#endif

                return null; /* SYSTEM DEFAULT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the specified environment variable for an
        /// overridden mask of global trace priorities and, when present and
        /// valid, returns it.
        /// </summary>
        /// <param name="envVarName">
        /// The name of the environment variable to check.
        /// </param>
        /// <returns>
        /// The overridden mask of global trace priorities when the environment
        /// variable is present and valid; otherwise, null.
        /// </returns>
        private static TracePriority? CheckForGlobalPriorities(
            string envVarName /* in */
            )
        {
            string stringValue = CommonOps.Environment.GetVariable(
                envVarName);

            if (stringValue == null)
                return null; /* NOT OVERRIDDEN */

            object enumValue;
            Result error = null;

            enumValue = EnumOps.TryParseFlags(
                null, typeof(TracePriority),
                DefaultGlobalPriorities.ToString(), stringValue,
                null, true, true, true, ref error);

            if (enumValue is TracePriority)
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultGlobalPriorities,
                    enumValue));
#endif

                return (TracePriority)enumValue;
            }
            else
            {
#if CONSOLE
                AppendInitializationMessage(String.Format(
                    _Constants.Prompt.DefaultGlobalPrioritiesError,
                    error));
#endif

                return null; /* SYSTEM DEFAULT */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any "trace" handling is currently
        /// possible.
        /// </summary>
        /// <returns>
        /// True if trace handling is possible; otherwise, false.
        /// </returns>
        private static bool IsTracePossible()
        {
            /* NO-LOCK */
            return isTracePossible && !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any "write" handling is currently
        /// possible.
        /// </summary>
        /// <returns>
        /// True if write handling is possible; otherwise, false.
        /// </returns>
        private static bool IsWritePossible()
        {
            /* NO-LOCK */
            return isWritePossible && !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method determines whether any trace operations are currently
        /// pending.
        /// </summary>
        /// <returns>
        /// True if one or more trace operations are pending; otherwise, false.
        /// </returns>
        private static bool IsTracePending()
        {
            return Interlocked.CompareExchange(ref traceLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any write operations are currently
        /// pending.
        /// </summary>
        /// <returns>
        /// True if one or more write operations are pending; otherwise, false.
        /// </returns>
        private static bool IsWritePending()
        {
            return Interlocked.CompareExchange(ref writeLevels, 0, 0) > 0;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the static lock is held.
        //
        /// <summary>
        /// This method creates a dictionary that maps each individual trace
        /// priority present in the specified mask to the specified value.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="priorities">
        /// The mask of trace priorities to include in the resulting dictionary.
        /// </param>
        /// <param name="value">
        /// The value to associate with each included trace priority.
        /// </param>
        /// <returns>
        /// A dictionary mapping each included trace priority to the specified
        /// value, or null when the set of known trace priorities is
        /// unavailable.
        /// </returns>
        public static TracePriorityDictionary CreateTracePriorities(
            TracePriority priorities, /* in */
            int value                 /* in */
            )
        {
            TracePriorityDictionary result = null;

            if (TracePriorities != null)
            {
                result = new TracePriorityDictionary();

                foreach (TracePriority priority in TracePriorities)
                    if (FlagOps.HasFlags(priorities, priority, true))
                        result.Add(priority, value);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the static lock is held.
        //
        /// <summary>
        /// This method finds the index, within the known set of trace
        /// priorities, of the lowest or highest priority present in the
        /// specified value.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The trace priority value to search for.
        /// </param>
        /// <param name="highest">
        /// Non-zero to find the highest matching priority; zero to find the
        /// lowest matching priority.
        /// </param>
        /// <returns>
        /// The index of the matching trace priority, or an invalid index when
        /// no match is found.
        /// </returns>
        private static int FindTracePriority(
            TracePriority priority, /* in */
            bool highest            /* in */
            )
        {
            if (TracePriorities != null)
            {
                int length = TracePriorities.Length;

                if (highest)
                {
                    for (int index = length - 1; index >= 0; index--)
                    {
                        TracePriority priorities = TracePriorities[index];

                        if (FlagOps.HasFlags(priority, priorities, true))
                            return index;
                    }
                }
                else
                {
                    for (int index = 0; index < length; index++)
                    {
                        TracePriority priorities = TracePriorities[index];

                        if (FlagOps.HasFlags(priority, priorities, true))
                            return index;
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the index of the first category, in the specified
        /// array, that is present (with a non-zero value) in the specified trace
        /// category dictionary, honoring the null-category allow and deny flags.
        /// </summary>
        /// <param name="priorities">
        /// The trace priority flags that control handling of a null category.
        /// </param>
        /// <param name="categories">
        /// The array of candidate trace categories to examine.
        /// </param>
        /// <param name="traceCategories">
        /// The dictionary of trace categories to search.
        /// </param>
        /// <returns>
        /// The index of the first matching category, or an invalid index when
        /// no match is found.
        /// </returns>
        private static int FindAnyTraceCategory(
            TracePriority priorities,     /* in */
            string[] categories,          /* in */
            IntDictionary traceCategories /* in */
            )
        {
            if ((categories != null) && (traceCategories != null))
            {
                int length = categories.Length;

                if (length > 0)
                {
                    bool denyNull = FlagOps.HasFlags(priorities,
                        TracePriority.DenyNullCategory, true);

                    bool allowNull = FlagOps.HasFlags(priorities,
                        TracePriority.AllowNullCategory, true);

                    for (int index = 0; index < length; index++)
                    {
                        string category = categories[index];

                        if (category == null)
                        {
                            if (denyNull)
                                break;    /* == Index.Invalid */

                            if (allowNull)
                                return 0; /* != Index.Invalid */

                            continue;
                        }

                        int value;

                        if (traceCategories.TryGetValue(
                                category, out value) &&
                            (value != 0))
                        {
                            return index;
                        }
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any category, in the specified array,
        /// is present (with a non-zero value) in the specified trace category
        /// dictionary.
        /// </summary>
        /// <param name="priorities">
        /// The trace priority flags that control handling of a null category.
        /// </param>
        /// <param name="categories">
        /// The array of candidate trace categories to examine.
        /// </param>
        /// <param name="traceCategories">
        /// The dictionary of trace categories to search.
        /// </param>
        /// <returns>
        /// True if any candidate category matches; otherwise, false.
        /// </returns>
        private static bool MatchAnyTraceCategory(
            TracePriority priorities,     /* in */
            string[] categories,          /* in */
            IntDictionary traceCategories /* in */
            )
        {
            return FindAnyTraceCategory(priorities,
                categories, traceCategories) != Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the static lock is held.
        //
        /// <summary>
        /// This method adjusts the base level of the specified trace priority up
        /// or down by the specified number of steps, clamping to the valid
        /// range.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to adjust, in place.
        /// </param>
        /// <param name="adjustment">
        /// The number of priority levels to adjust by; positive values increase
        /// the priority and negative values decrease it.
        /// </param>
        private static void AdjustTracePriority(
            ref TracePriority priority, /* in, out */
            int adjustment              /* in */
            )
        {
            int oldIndex = FindTracePriority(
                priority, adjustment > 0);

            if (oldIndex == Index.Invalid)
                return;

            if (TracePriorities != null)
            {
                int length = TracePriorities.Length;

                if (length > 0)
                {
                    int newIndex = oldIndex;

                    if (adjustment != 0)
                        newIndex += adjustment;

                    if (newIndex < 0)
                        newIndex = 0;

                    if (newIndex >= length)
                        newIndex = length - 1;

                    priority &= ~TracePriorities[oldIndex];
                    priority |= TracePriorities[newIndex];
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adjusts the base level of the specified trace priority up
        /// or down by the specified number of steps while holding the static
        /// lock.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to adjust, in place.
        /// </param>
        /// <param name="adjustment">
        /// The number of priority levels to adjust by; positive values increase
        /// the priority and negative values decrease it.
        /// </param>
        public static void ExternalAdjustTracePriority(
            ref TracePriority priority, /* in, out */
            int adjustment              /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                AdjustTracePriority(ref priority, adjustment);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method masks the specified trace priority down to only its
        /// priority-level bits.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to mask.
        /// </param>
        /// <returns>
        /// The masked trace priority, containing only its priority-level bits.
        /// </returns>
        public static TracePriority MaskTracePriority(
            TracePriority priority /* in */
            )
        {
            return priority & TracePriority.AnyPriorityMask;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the priority-level bits of the specified trace
        /// priority with those of the specified base priority, when the base
        /// priority contains any priority-level bits.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to modify, in place.
        /// </param>
        /// <param name="basePriority">
        /// The base trace priority whose priority-level bits are used.
        /// </param>
        public static void ChangeBaseTracePriority(
            ref TracePriority priority, /* in, out */
            TracePriority basePriority  /* in */
            )
        {
            TracePriority newBasePriority = MaskTracePriority(
                basePriority);

            if (FlagOps.HasFlags(newBasePriority,
                    TracePriority.AnyPriorityMask, false))
            {
                priority &= ~TracePriority.AnyPriorityMask;
                priority |= newBasePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the core type bits of the specified trace
        /// priority to indicate an error.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to modify, in place.
        /// </param>
        public static void ChangeToErrorPriority(
            ref TracePriority priority /* in, out */
            )
        {
            priority &= ~TracePriority.AnyCoreTypeMask;
            priority |= TracePriority.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the display name corresponding to the specified
        /// trace priority, in either its short or full form.
        /// </summary>
        /// <param name="priority">
        /// The trace priority whose name is requested.
        /// </param>
        /// <param name="shortName">
        /// Non-zero to return the short name; zero to return the full name.
        /// </param>
        /// <returns>
        /// The display name of the trace priority, the empty string when no
        /// name applies, or null when no matching priority is found.
        /// </returns>
        public static string GetTracePriorityName(
            TracePriority priority, /* in */
            bool shortName          /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (FlagOps.HasFlags(priority, TracePriority.Never, true))
                {
                    if (shortName)
                        return NeverTracePriorityShortName;
                    else
                        return NeverTracePriorityFullName;
                }
                else if (FlagOps.HasFlags(priority, TracePriority.Always, true))
                {
                    if (shortName)
                        return AlwaysTracePriorityShortName;
                    else
                        return AlwaysTracePriorityFullName;
                }
                else
                {
                    int index = FindTracePriority(priority, false);

                    if (index == Index.Invalid)
                        return null;

                    if (shortName)
                    {
                        if (TracePriorityShortNames != null)
                        {
                            int length = TracePriorityShortNames.Length;

                            if (length > 0)
                                return TracePriorityShortNames[index];
                        }
                    }
                    else
                    {
                        if (TracePriorityFullNames != null)
                        {
                            int length = TracePriorityFullNames.Length;

                            if (length > 0)
                                return TracePriorityFullNames[index];
                        }
                    }
                }

                return String.Empty;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is used to check if the specified set of trace
        //       priorities and types are enabled.  Any enumeration values
        //       that are not in the subsets used for priority and/or type
        //       are excluded from this checking (e.g. EnableDateTimeFlag,
        //       CategoryPenalty, User0, etc).
        //
        /// <summary>
        /// This method checks whether the specified set of trace priority flags
        /// is present, considering only those bits that participate in priority
        /// and type checking.
        /// </summary>
        /// <param name="flags">
        /// The trace priority flags to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The trace priority flags to look for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require all of the requested flags; zero to require any
        /// of them.
        /// </param>
        /// <returns>
        /// True if the requested flags are present according to
        /// <paramref name="all" />; otherwise, false.
        /// </returns>
        private static bool HasTracePriorities(
            TracePriority flags,    /* in */
            TracePriority hasFlags, /* in */
            bool all                /* in */
            )
        {
            return FlagOps.HasFlags(
                flags, hasFlags & TracePriority.HasPrioritiesMask,
                all); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any trace categories (enabled,
        /// disabled, penalty, or bonus) are currently configured.
        /// </summary>
        /// <returns>
        /// True if at least one trace category is configured; otherwise, false.
        /// </returns>
        private static bool HaveTraceCategories()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (enabledTraceCategories != null)
                    count += enabledTraceCategories.Count;

                if (disabledTraceCategories != null)
                    count += disabledTraceCategories.Count;

                if (penaltyTraceCategories != null)
                    count += penaltyTraceCategories.Count;

                if (bonusTraceCategories != null)
                    count += bonusTraceCategories.Count;

                return (count > 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the trace category and related checks
        /// can be skipped, given a method name and the caller's preference.
        /// </summary>
        /// <param name="methodName">
        /// The method name associated with the trace message, if any.
        /// </param>
        /// <param name="skipChecks">
        /// The caller's preference for skipping checks when no method name is
        /// available.
        /// </param>
        /// <returns>
        /// True if the checks can be skipped; otherwise, false.
        /// </returns>
        private static bool CanSkipChecks(
            string methodName, /* in */
            bool skipChecks    /* in */
            )
        {
            //
            // TODO: This method assumes that checks can be skipped if there
            //       is no method name -OR- there are no trace categories.
            //       The idea (from within DebugTraceRaw) is that all checks
            //       have already been performed by (one of) the callers,
            //       *except* for the method name, if any.  Of course, this
            //       may need to be changed in the future.
            //
            if (String.IsNullOrEmpty(methodName))
                return skipChecks;

            return !HaveTraceCategories();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the tracing subsystem should suppress
        /// its initialization messages, based solely on the process
        /// environment.
        /// </summary>
        /// <returns>
        /// True if initialization messages should be suppressed; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldBeQuiet()
        {
            //
            // HACK: No interpreter context is used here.  Just rely on the
            //       process environment.
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.Quiet))
                return true;

            if (CommonOps.Environment.DoesVariableExist(EnvVars.DefaultQuiet))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the static field that indicates whether
        /// tracing is enabled, based on the relevant environment variables
        /// (and their associated defaults).  This method assumes the static
        /// lock is held.
        /// </summary>
        /// <param name="quiet">
        /// Non-zero to suppress any initialization messages that would
        /// otherwise be emitted while initializing the trace enabled state.
        /// </param>
        private static void InitializeTraceEnabled(
            bool quiet /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Cannot use any GlobalConfiguration methods at this
                //       point because those methods could call into one of
                //       our DebugTrace methods (below), which could end up
                //       indirectly calling into this method again.
                //
                if (CommonOps.Environment.DoesVariableExist(EnvVars.NoTrace))
                {
#if CONSOLE
                    if (!quiet)
                    {
                        AppendInitializationMessage(
                            _Constants.Prompt.NoTraceOps);
                    }
#endif

                    isTraceEnabled = false;
                }
                else if (CommonOps.Environment.DoesVariableExist(EnvVars.Trace))
                {
#if CONSOLE
                    if (!quiet)
                    {
                        AppendInitializationMessage(
                            _Constants.Prompt.TraceOps);
                    }
#endif

                    isTraceEnabled = true;
                }
                else
                {
                    if (isTraceEnabledByDefault == null)
                        isTraceEnabledByDefault = DefaultTraceEnabledByDefault;

                    isTraceEnabled = isTraceEnabledByDefault;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is part of a hack that solves a chicken-and-egg problem
        //       with the diagnostic tracing method used by this library.  We
        //       allow tracing to be disabled via an environment variable
        //       and/or the shell command line.  Unfortunately, by the time we
        //       disable tracing, many messages will have typically already
        //       been written to the trace listeners.  To prevent this noise
        //       (that the user wishes to suppress), we internalize the check
        //       (i.e. we do it from inside the core trace method itself) and
        //       initialize this variable [once] with the result of checking
        //       the environment variable.
        //
        /// <summary>
        /// This method determines whether a trace message with the specified
        /// priority and categories should be allowed through, based on the
        /// global trace enabled state, the configured trace priority masks,
        /// and the sets of enabled, disabled, bonus, and penalty trace
        /// categories.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="categories">
        /// The trace categories associated with the trace message, if any.
        /// </param>
        /// <returns>
        /// True if a trace message with the specified priority and categories
        /// should be allowed through; otherwise, false.
        /// </returns>
        private static bool IsTraceEnabled(
            TracePriority priority,    /* in */
            params string[] categories /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Does the enabled flag still need to be initialized?
                //
                if (isTraceEnabled == null)
                {
                    //
                    // NOTE: Ok, attempt to initialize it now, keeping quiet
                    //       about it if necessary.
                    //
                    InitializeTraceEnabled(ShouldBeQuiet());

                    //
                    // HACK: *FAIL-SAFE* If our static field is *still* null,
                    //       just return false now.
                    //
                    if (isTraceEnabled == null)
                        return false;
                }

                //
                // NOTE: Determine if tracing is globally enabled or disabled.
                //
                bool result = (bool)isTraceEnabled;

                //
                // NOTE: If tracing has been globally disabled, do not bother
                //       checking any categories.
                //
                if (result)
                {
                    /* NO RESULT */
                    MaybeInitialize();

                    //
                    // NOTE: Cache the configured trace priority masks used by
                    //       this method.
                    //
                    TracePriority tracePriorities = GetTracePriorities();
                    TracePriority globalPriorities = GetGlobalPriorities();

                    //
                    // NOTE: Initially, there are no priority adjustments; they
                    //       may be adjusted based on the set of categories for
                    //       this message.
                    //
                    int categoryPenalty = 0;
                    int categoryBonus = 0;

                retry:

                    //
                    // NOTE: If the category penalty adjustment is non-zero,
                    //       use it to adjust the priority.
                    //
                    if (categoryPenalty != 0)
                        AdjustTracePriority(ref priority, categoryPenalty);

                    //
                    // NOTE: If the category bonus adjustment is non-zero,
                    //       use it to adjust the priority.
                    //
                    if (categoryBonus != 0)
                        AdjustTracePriority(ref priority, categoryBonus);

                    //
                    // NOTE: If the "Never" flag is set within the priority
                    //       then all remaining checks will be skipped -AND-
                    //       this flag will ALWAYS be honored forevermore.
                    //
                    if (HasTracePriorities(globalPriorities | priority,
                            TracePriority.Never, true))
                    {
                        result = false;
                        goto done;
                    }

                    //
                    // NOTE: If the "Always" flag is set within the priority
                    //       then all remaining checks will be skipped -AND-
                    //       this flag will ALWAYS be honored forevermore.
                    //
                    if (HasTracePriorities(globalPriorities | priority,
                            TracePriority.Always, true))
                    {
                        goto done;
                    }

                    //
                    // NOTE: The priority flags specified by the caller must
                    //       all be present in the configured trace priority
                    //       flags.
                    //
                    if (!HasTracePriorities(tracePriorities, priority, true))
                    {
                        //
                        // NOTE: The priority specified by the caller may need
                        //       a "bonus" based on the set of categories for
                        //       this message.
                        //
                        if ((categoryBonus == 0) &&
                            (bonusTraceCategories != null) &&
                            (bonusTraceCategories.Count > 0) &&
                            FlagOps.HasFlags(tracePriorities,
                                    TracePriority.CategoryBonus, true) &&
                            MatchAnyTraceCategory(tracePriorities,
                                    categories, bonusTraceCategories))
                        {
                            categoryBonus = DefaultCategoryBonus;

                            if (categoryBonus != 0)
                                goto retry;
                        }

                        result = false;
                    }
                    else
                    {
                        //
                        // NOTE: The priority specified by the caller may need
                        //       a "penalty" based on the set of categories for
                        //       this message.
                        //
                        if ((categoryPenalty == 0) &&
                            (penaltyTraceCategories != null) &&
                            (penaltyTraceCategories.Count > 0) &&
                            FlagOps.HasFlags(tracePriorities,
                                    TracePriority.CategoryPenalty, true) &&
                            MatchAnyTraceCategory(tracePriorities,
                                    categories, penaltyTraceCategories))
                        {
                            categoryPenalty = DefaultCategoryPenalty;

                            if (categoryPenalty != 0)
                                goto retry;
                        }

                        //
                        // NOTE: If the caller specified a null category -OR-
                        //       there are no trace categories specifically
                        //       enabled (i.e. all trace categories are
                        //       allowed), always allow the message through.
                        //
                        if (result &&
                            (categories != null) && (categories.Length > 0))
                        {
                            if (result &&
                                (enabledTraceCategories != null) &&
                                (enabledTraceCategories.Count > 0))
                            {
                                //
                                // NOTE: At this point, at least one of the
                                //       specified trace categories for this
                                //       message must exist in the dictionary
                                //       of enabled trace categories and its
                                //       associated value must be non-zero;
                                //       otherwise, the trace message is not
                                //       allowed through.
                                //
                                if (!MatchAnyTraceCategory(
                                        tracePriorities, categories,
                                        enabledTraceCategories))
                                {
                                    result = false;
                                }
                            }

                            if (result &&
                                (disabledTraceCategories != null) &&
                                (disabledTraceCategories.Count > 0))
                            {
                                //
                                // NOTE: At this point, none of the specified
                                //       trace categories for this message can
                                //       exist in the dictionary of disabled
                                //       trace categories or their associated
                                //       values must be zero; otherwise, the
                                //       trace message is not allowed through.
                                //
                                if (MatchAnyTraceCategory(
                                        tracePriorities, categories,
                                        disabledTraceCategories))
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                }

            done:

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether trace messages should be written to
        /// the active interpreter host (when available) instead of using the
        /// default trace output handling.
        /// </summary>
        /// <returns>
        /// True if trace messages should be written to the active interpreter
        /// host; otherwise, false.
        /// </returns>
        private static bool GetTraceToInterpreterHost()
        {
            return Interlocked.CompareExchange(
                ref isTraceToInterpreterHost, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables writing trace messages to the
        /// active interpreter host by adjusting the associated reference
        /// count.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to enable writing trace messages to the active interpreter
        /// host; zero to disable it.
        /// </param>
        /// <returns>
        /// True if writing trace messages to the active interpreter host is
        /// (still) enabled after the adjustment; otherwise, false.
        /// </returns>
        private static bool SetTraceToInterpreterHost(
            bool enabled /* in */
            )
        {
            if (enabled)
            {
                return Interlocked.Increment(
                    ref isTraceToInterpreterHost) > 0;
            }
            else
            {
                return Interlocked.Decrement(
                    ref isTraceToInterpreterHost) > 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the flag that indicates whether trace handling is
        /// currently possible.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero if trace handling should be considered possible; otherwise,
        /// zero.
        /// </param>
        private static void SetTracePossible(
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                isTracePossible = enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the flag that indicates whether trace handling
        /// is currently possible back to its default value.  This method
        /// assumes the static lock is held.
        /// </summary>
        private static void ResetTracePossible()
        {
            lock (syncRoot)
            {
                isTracePossible = DefaultTracePossible;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the flag that indicates whether write handling is
        /// currently possible.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero if write handling should be considered possible; otherwise,
        /// zero.
        /// </param>
        private static void SetWritePossible( /* NOT USED */
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                isWritePossible = enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the flag that indicates whether write handling
        /// is currently possible back to its default value.  This method
        /// assumes the static lock is held.
        /// </summary>
        private static void ResetWritePossible() /* NOT USED */
        {
            lock (syncRoot)
            {
                isWritePossible = DefaultWritePossible;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method explicitly sets the flag that indicates whether tracing
        /// is enabled.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to enable tracing; zero to disable it.
        /// </param>
        private static void SetTraceEnabled(
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                isTraceEnabled = enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the flag that indicates whether tracing is
        /// enabled to null, forcing it to be re-initialized upon next use.
        /// This method assumes the static lock is held.
        /// </summary>
        private static void ResetTraceEnabled()
        {
            lock (syncRoot)
            {
                isTraceEnabled = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally increases the number of call frames to
        /// skip when capturing a stack trace, based on the specified trace
        /// priority flags, so that internal wrapper methods are excluded from
        /// the captured stack trace.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of call frames to skip; upon return, this value may have
        /// been increased based on the specified trace priority flags.
        /// </param>
        private static void MaybeAdjustSkipFrames(
            TracePriority priority, /* in */
            ref int skipFrames      /* in, out */
            )
        {
            if (FlagOps.HasFlags(
                    priority, TracePriority.External, true))
            {
                //
                // NOTE: When this subsystem is called via the external
                //       public methods in the Utility class, make sure
                //       that public wrapper method is excluded from the
                //       included stack trace, if any.
                //
                skipFrames++;
            }

            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    priority, TracePriority.ExtraSkipFrame, true))
            {
                //
                // NOTE: When conditional DebugTrace methods call into
                //       unconditional DebugTrace methods, add an extra
                //       skipped call frame.
                //
                skipFrames++;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Trace Filter Management
        /// <summary>
        /// This method gets the effective trace filter callback, preferring the
        /// one associated with the specified interpreter (if any) and falling
        /// back to the globally configured trace filter callback.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose associated trace filter callback should be
        /// used, if available.  This value may be null.
        /// </param>
        /// <returns>
        /// The effective trace filter callback, or null if there is none.
        /// </returns>
        private static TraceFilterCallback GetTraceFilterCallback(
            Interpreter interpreter /* in */
            )
        {
            TraceFilterCallback callback = null;

            if (interpreter != null)
                callback = interpreter.InternalTraceFilterCallback;

            if (callback == null)
                callback = GetTraceFilterCallback();

            return callback;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the globally configured trace filter callback.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The globally configured trace filter callback, or null if there is
        /// none.
        /// </returns>
        private static TraceFilterCallback GetTraceFilterCallback()
        {
            lock (syncRoot)
            {
                return traceFilterCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the globally configured trace filter callback.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <param name="callback">
        /// The trace filter callback to use globally.  This value may be null.
        /// </param>
        public static void SetTraceFilterCallback(
            TraceFilterCallback callback /* in */
            )
        {
            lock (syncRoot)
            {
                traceFilterCallback = callback;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the globally configured trace filter callback,
        /// removing it.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTraceFilterCallback()
        {
            lock (syncRoot)
            {
                traceFilterCallback = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the trace filter callback associated with the
        /// specified interpreter, removing it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose associated trace filter callback should be
        /// removed.  This value may be null.
        /// </param>
        private static void ResetTraceFilterCallback(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter != null)
                interpreter.InternalTraceFilterCallback = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the effective trace filter callback, if any, to
        /// determine whether the specified trace message should be filtered
        /// out (i.e. dropped).  Any exception thrown by the callback is caught
        /// and ignored, in which case the message is not filtered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message; the callback may modify this value.
        /// </param>
        /// <param name="category">
        /// The trace category; the callback may modify this value.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags; the callback may modify this value.
        /// </param>
        /// <returns>
        /// True if the trace message should be filtered out (i.e. dropped);
        /// otherwise, false.
        /// </returns>
        private static bool IsTraceFiltered(
            Interpreter interpreter,   /* in */
            ref string message,        /* in */
            ref string category,       /* in */
            ref TracePriority priority /* in */
            )
        {
            try
            {
                //
                // HACK: Attempt to invoke the trace filter callback.  If
                //       it throws an exception, we ignore it -AND- allow
                //       the trace to be written.  Exceptions here cannot
                //       be allowed to escape this method because that may
                //       cause this class to be called again to report the
                //       exception.
                //
                TraceFilterCallback callback = GetTraceFilterCallback(
                    interpreter);

                if (callback != null)
                {
                    return callback(
                        interpreter, ref message, ref category,
                        ref priority); /* throw */
                }
            }
            catch
            {
                Interlocked.Increment(ref traceException);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Priority Management
        /// <summary>
        /// This method gets the configured trace priority mask, which controls
        /// the set of trace priority flags that are currently allowed.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The configured trace priority mask.
        /// </returns>
        public static TracePriority GetTracePriorities()
        {
            lock (syncRoot)
            {
                return tracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the configured trace priority mask, which controls
        /// the set of trace priority flags that are currently allowed.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="priorities">
        /// The new trace priority mask.
        /// </param>
        public static void SetTracePriorities(
            TracePriority priorities /* in */
            )
        {
            lock (syncRoot)
            {
                tracePriorities = priorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds or removes the specified trace priority flags
        /// from the configured trace priority mask.  This method assumes the
        /// static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags to add or remove.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to add the specified flags; zero to remove them.
        /// </param>
        public static void AdjustTracePriorities(
            TracePriority priority, /* in */
            bool enabled            /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (enabled)
                    tracePriorities |= priority;
                else
                    tracePriorities &= ~priority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the configured trace priority mask back to its
        /// default value.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTracePriorities()
        {
            lock (syncRoot)
            {
                tracePriorities = DefaultTracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the configured trace priority mask from the
        /// relevant environment variable (and/or its default), if it has not
        /// already been set.  Exceptions are caught and ignored.  This method
        /// assumes the static lock is held.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re-)initialization even when the trace priority
        /// mask already has a non-default value.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to fall back to the default trace priority mask when the
        /// relevant environment variable is not present.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which portions of
        /// the trace state, if any, were initialized.
        /// </returns>
        private static TraceStateType InitializeTracePriorities(
            bool force,      /* in */
            bool useDefaults /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (force || (tracePriorities == TracePriority.None))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        TracePriority? priorities = CheckForTracePriorities(
                            EnvVars.TracePriorities);

                        if (priorities != null)
                        {
                            tracePriorities = (TracePriority)priorities;
                            result |= TraceStateType.Priorities;
                        }
                        else if (useDefaults)
                        {
                            tracePriorities = DefaultTracePriorities;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the configured global trace priority mask, which is
        /// combined with the per-message priority to force certain flags (such
        /// as "Always" and "Never") on or off.  This method assumes the static
        /// lock is held.
        /// </summary>
        /// <returns>
        /// The configured global trace priority mask.
        /// </returns>
        public static TracePriority GetGlobalPriorities()
        {
            lock (syncRoot)
            {
                return globalPriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the configured global trace priority mask.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="priorities">
        /// The new global trace priority mask.
        /// </param>
        public static void SetGlobalPriorities(
            TracePriority priorities /* in */
            )
        {
            lock (syncRoot)
            {
                globalPriorities = priorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds or removes the specified trace priority flags from
        /// the configured global trace priority mask.  This method assumes the
        /// static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags to add or remove.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to add the specified flags; zero to remove them.
        /// </param>
        public static void AdjustGlobalPriorities(
            TracePriority priority, /* in */
            bool enabled            /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (enabled)
                    globalPriorities |= priority;
                else
                    globalPriorities &= ~priority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the configured global trace priority mask back
        /// to its default value.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetGlobalPriorities()
        {
            lock (syncRoot)
            {
                globalPriorities = DefaultGlobalPriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the configured global trace priority mask
        /// from the relevant environment variable (and/or its default), if it
        /// has not already been set.  Exceptions are caught and ignored.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re-)initialization even when the global trace
        /// priority mask already has a non-default value.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to fall back to the default global trace priority mask when
        /// the relevant environment variable is not present.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which portions of
        /// the trace state, if any, were initialized.
        /// </returns>
        private static TraceStateType InitializeGlobalPriorities(
            bool force,      /* in */
            bool useDefaults /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (force || (globalPriorities == TracePriority.None))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        TracePriority? priorities = CheckForGlobalPriorities(
                            EnvVars.GlobalPriorities);

                        if (priorities != null)
                        {
                            globalPriorities = (TracePriority)priorities;
                            result |= TraceStateType.Priorities;
                        }
                        else if (useDefaults)
                        {
                            globalPriorities = DefaultGlobalPriorities;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default trace priority, which is used for trace
        /// messages that do not specify one explicitly.  This method assumes
        /// the static lock is held.
        /// </summary>
        /// <returns>
        /// The default trace priority.
        /// </returns>
        public static TracePriority GetTracePriority()
        {
            lock (syncRoot)
            {
                return defaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the default trace priority, which is used for trace
        /// messages that do not specify one explicitly.  This method assumes
        /// the static lock is held.
        /// </summary>
        /// <param name="priority">
        /// The new default trace priority.
        /// </param>
        public static void SetTracePriority(
            TracePriority priority /* in */
            )
        {
            lock (syncRoot)
            {
                defaultTracePriority = priority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the default trace priority back to its default
        /// value.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTracePriority()
        {
            lock (syncRoot)
            {
                defaultTracePriority = DefaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the default trace priority from the relevant
        /// environment variable (and/or its default), if it has not already
        /// been set.  Exceptions are caught and ignored.  This method assumes
        /// the static lock is held.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re-)initialization even when the default trace
        /// priority already has a non-default value.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to fall back to the default trace priority when the
        /// relevant environment variable is not present.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which portions of
        /// the trace state, if any, were initialized.
        /// </returns>
        private static TraceStateType InitializeTracePriority(
            bool force,      /* in */
            bool useDefaults /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (force || (defaultTracePriority == TracePriority.None))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        TracePriority? priority = CheckForTracePriority(
                            EnvVars.TracePriority);

                        if (priority != null)
                        {
                            defaultTracePriority = (TracePriority)priority;
                            result |= TraceStateType.Priority;
                        }
                        else if (useDefaults)
                        {
                            defaultTracePriority = DefaultTracePriority;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates any trace format selection flags present in
        /// the configured trace priority mask into the corresponding trace
        /// format string, clearing those flags as they are processed.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The number of trace format selection flags that were processed.
        /// </returns>
        private static int TracePrioritiesToFormatString()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (FlagOps.HasFlags(tracePriorities,
                        TracePriority.EnableMinimumFormatFlag, true) &&
                    !IsMinimumTraceFormat())
                {
                    tracePriorities &= ~TracePriority.EnableMinimumFormatFlag;

                    if (SetMinimumTraceFormat())
                        result++;
                }

                if (FlagOps.HasFlags(tracePriorities,
                        TracePriority.EnableMediumLowFormatFlag, true) &&
                    !IsMediumLowTraceFormat())
                {
                    tracePriorities &= ~TracePriority.EnableMediumLowFormatFlag;

                    if (SetMediumLowTraceFormat())
                        result++;
                }

                if (FlagOps.HasFlags(tracePriorities,
                        TracePriority.EnableMediumFormatFlag, true) &&
                    !IsMediumTraceFormat())
                {
                    tracePriorities &= ~TracePriority.EnableMediumFormatFlag;

                    if (SetMediumTraceFormat())
                        result++;
                }

                if (FlagOps.HasFlags(tracePriorities,
                        TracePriority.EnableMediumHighFormatFlag, true) &&
                    !IsMediumHighTraceFormat())
                {
                    tracePriorities &= ~TracePriority.EnableMediumHighFormatFlag;

                    if (SetMediumHighTraceFormat())
                        result++;
                }

                if (FlagOps.HasFlags(tracePriorities,
                        TracePriority.EnableMaximumFormatFlag, true) &&
                    !IsMaximumTraceFormat())
                {
                    tracePriorities &= ~TracePriority.EnableMaximumFormatFlag;

                    if (SetMaximumTraceFormat())
                        result++;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates any trace format flag adjustments present in
        /// the configured trace priority mask into the corresponding trace
        /// format flag fields, clearing those flags as they are processed.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The number of trace format flag adjustments that were processed.
        /// </returns>
        private static int TracePrioritiesToFormatFlags()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return TracePrioritiesToFormatFlags(
                    ref tracePriorities, ref traceDateTime,
                    ref tracePriority, ref traceServerName,
                    ref traceTestName, ref traceAppDomain,
                    ref traceInterpreter, ref traceThreadId,
                    ref traceMethod, ref traceStack,
                    ref traceExtraNewLines);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method translates any trace format flag adjustments present in
        /// the specified trace priority mask into the corresponding trace
        /// format flag fields, clearing those flags from the mask as they are
        /// processed.
        /// </summary>
        /// <param name="priorities">
        /// The trace priority mask to examine; any trace format flag
        /// adjustments that are processed are cleared from this value.
        /// </param>
        /// <param name="zero">
        /// Upon return, may be set to enable inclusion of the date and time in
        /// the trace output.
        /// </param>
        /// <param name="one">
        /// Upon return, may be set to enable inclusion of the trace priority in
        /// the trace output.
        /// </param>
        /// <param name="two">
        /// Upon return, may be set to enable inclusion of the server name in
        /// the trace output.
        /// </param>
        /// <param name="three">
        /// Upon return, may be set to enable inclusion of the test name in the
        /// trace output.
        /// </param>
        /// <param name="four">
        /// Upon return, may be set to enable inclusion of the application
        /// domain in the trace output.
        /// </param>
        /// <param name="five">
        /// Upon return, may be set or cleared to enable or disable inclusion of
        /// the interpreter in the trace output.
        /// </param>
        /// <param name="six">
        /// Upon return, may be set to enable inclusion of the thread
        /// identifier in the trace output.
        /// </param>
        /// <param name="seven">
        /// Upon return, may be set to enable inclusion of the method name in
        /// the trace output.
        /// </param>
        /// <param name="eight">
        /// Upon return, may be set to enable inclusion of the stack trace in
        /// the trace output.
        /// </param>
        /// <param name="nine">
        /// Upon return, may be set to enable inclusion of extra new lines in
        /// the trace output.
        /// </param>
        /// <returns>
        /// The number of trace format flag adjustments that were processed.
        /// </returns>
        private static int TracePrioritiesToFormatFlags(
            ref TracePriority priorities, /* in, out */
            ref bool zero,                /* in, out */
            ref bool one,                 /* in, out */
            ref bool two,                 /* in, out */
            ref bool three,               /* in, out */
            ref bool four,                /* in, out */
            ref bool five,                /* in, out */
            ref bool six,                 /* in, out */
            ref bool seven,               /* in, out */
            ref bool eight,               /* in, out */
            ref bool nine                 /* in, out */
            )
        {
            int result = 0;

            if (!zero && FlagOps.HasFlags(priorities,
                    TracePriority.EnableDateTimeFlag, true))
            {
                priorities &= ~TracePriority.EnableDateTimeFlag;
                zero = true;
                result++;
            }

            if (!one && FlagOps.HasFlags(priorities,
                    TracePriority.EnablePriorityFlag, true))
            {
                priorities &= ~TracePriority.EnablePriorityFlag;
                one = true;
                result++;
            }

            if (!two && FlagOps.HasFlags(priorities,
                    TracePriority.EnableServerNameFlag, true))
            {
                priorities &= ~TracePriority.EnableServerNameFlag;
                two = true;
                result++;
            }

            if (!three && FlagOps.HasFlags(priorities,
                    TracePriority.EnableTestNameFlag, true))
            {
                priorities &= ~TracePriority.EnableTestNameFlag;
                three = true;
                result++;
            }

            if (!four && FlagOps.HasFlags(priorities,
                    TracePriority.EnableAppDomainFlag, true))
            {
                priorities &= ~TracePriority.EnableAppDomainFlag;
                four = true;
                result++;
            }

            if (!five && FlagOps.HasFlags(priorities,
                    TracePriority.EnableInterpreterFlag, true))
            {
                priorities &= ~TracePriority.EnableInterpreterFlag;
                five = true;
                result++;
            }

            if (five && FlagOps.HasFlags(priorities,
                    TracePriority.DisableInterpreterFlag, true))
            {
                priorities &= ~TracePriority.DisableInterpreterFlag;
                five = false;
                result++;
            }

            if (!six && FlagOps.HasFlags(priorities,
                    TracePriority.EnableThreadIdFlag, true))
            {
                priorities &= ~TracePriority.EnableThreadIdFlag;
                six = true;
                result++;
            }

            if (!seven && FlagOps.HasFlags(priorities,
                    TracePriority.EnableMethodFlag, true))
            {
                priorities &= ~TracePriority.EnableMethodFlag;
                seven = true;
                result++;
            }

            if (!eight && FlagOps.HasFlags(priorities,
                    TracePriority.EnableStackFlag, true))
            {
                priorities &= ~TracePriority.EnableStackFlag;
                eight = true;
                result++;
            }

            if (!nine && FlagOps.HasFlags(priorities,
                    TracePriority.EnableExtraNewLinesFlag, true))
            {
                priorities &= ~TracePriority.EnableExtraNewLinesFlag;
                nine = true;
                result++;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Category Management
        /// <summary>
        /// This method lists the default set of trace categories along with
        /// their associated values.
        /// </summary>
        /// <returns>
        /// A collection of strings describing the trace categories, or null if
        /// there are none.
        /// </returns>
        private static IEnumerable<string> ListTraceCategories()
        {
            return ListTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method lists the trace categories of the specified type(s)
        /// (enabled, disabled, penalty, and/or bonus) along with their
        /// associated values.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="categoryType">
        /// The type(s) of trace categories to include in the resulting list.
        /// </param>
        /// <returns>
        /// A collection of strings describing the trace categories, or null if
        /// there are none.
        /// </returns>
        private static IEnumerable<string> ListTraceCategories(
            TraceCategoryType categoryType /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;
                StringList enabledCategories = null;

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Enabled, true) &&
                    (enabledTraceCategories != null))
                {
                    foreach (KeyValuePair<string, int> pair
                            in enabledTraceCategories)
                    {
                        if (enabledCategories == null)
                            enabledCategories = new StringList();

                        enabledCategories.Add(
                            pair.Key, pair.Value.ToString());
                    }

                    if (enabledCategories != null)
                        count++;
                }

                StringList disabledCategories = null;

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Disabled, true) &&
                    (disabledTraceCategories != null))
                {
                    foreach (KeyValuePair<string, int> pair
                            in disabledTraceCategories)
                    {
                        if (disabledCategories == null)
                            disabledCategories = new StringList();

                        disabledCategories.Add(
                            pair.Key, pair.Value.ToString());
                    }

                    if (disabledCategories != null)
                        count++;
                }

                StringList penaltyCategories = null;

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Penalty, true) &&
                    (penaltyTraceCategories != null))
                {
                    foreach (KeyValuePair<string, int> pair
                            in penaltyTraceCategories)
                    {
                        if (penaltyCategories == null)
                            penaltyCategories = new StringList();

                        penaltyCategories.Add(
                            pair.Key, pair.Value.ToString());
                    }

                    if (penaltyCategories != null)
                        count++;
                }

                StringList bonusCategories = null;

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Bonus, true) &&
                    (bonusTraceCategories != null))
                {
                    foreach (KeyValuePair<string, int> pair
                            in bonusTraceCategories)
                    {
                        if (bonusCategories == null)
                            bonusCategories = new StringList();

                        bonusCategories.Add(
                            pair.Key, pair.Value.ToString());
                    }

                    if (bonusCategories != null)
                        count++;
                }

                StringList categories = null;

                if (count > 0)
                {
                    if (enabledCategories != null)
                    {
                        if (count > 1)
                        {
                            if (categories == null)
                                categories = new StringList();

                            categories.Add("enabled");

                            categories.Add(
                                enabledCategories.ToString());
                        }
                        else
                        {
                            categories = enabledCategories;
                        }
                    }

                    if (disabledCategories != null)
                    {
                        if (count > 1)
                        {
                            if (categories == null)
                                categories = new StringList();

                            categories.Add("disabled");

                            categories.Add(
                                disabledCategories.ToString());
                        }
                        else
                        {
                            categories = disabledCategories;
                        }
                    }

                    if (penaltyCategories != null)
                    {
                        if (count > 1)
                        {
                            if (categories == null)
                                categories = new StringList();

                            categories.Add("penalty");

                            categories.Add(
                                penaltyCategories.ToString());
                        }
                        else
                        {
                            categories = penaltyCategories;
                        }
                    }

                    if (bonusCategories != null)
                    {
                        if (count > 1)
                        {
                            if (categories == null)
                                categories = new StringList();

                            categories.Add("bonus");

                            categories.Add(
                                bonusCategories.ToString());
                        }
                        else
                        {
                            categories = bonusCategories;
                        }
                    }
                }

                return categories;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds (or adjusts the value of) the specified categories
        /// in the default set of trace categories.
        /// </summary>
        /// <param name="categories">
        /// The trace categories to add or adjust.  This value may be null.
        /// </param>
        /// <param name="value">
        /// The value to associate with each trace category; if a category
        /// already exists, this value is added to its existing value.
        /// </param>
        public static void SetTraceCategories(
            IEnumerable<string> categories, /* in */
            int value                       /* in */
            )
        {
            SetTraceCategories(TraceCategoryType.Default, categories, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds (or adjusts the value of) the specified categories
        /// in the trace category dictionaries of the specified type(s).  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="categoryType">
        /// The type(s) of trace category dictionaries to add or adjust.
        /// </param>
        /// <param name="categories">
        /// The trace categories to add or adjust.  This value may be null.
        /// </param>
        /// <param name="value">
        /// The value to associate with each trace category; if a category
        /// already exists, this value is added to its existing value.
        /// </param>
        public static void SetTraceCategories(
            TraceCategoryType categoryType, /* in */
            IEnumerable<string> categories, /* in */
            int value                       /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Enabled, true))
                {
                    //
                    // NOTE: If the dictionary of "enabled" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (enabledTraceCategories == null)
                        enabledTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be added to.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Add or modify the trace category.
                            //
                            int oldValue;

                            if (enabledTraceCategories.TryGetValue(
                                    category, out oldValue))
                            {
                                value += oldValue;
                            }

                            enabledTraceCategories[category] = value;
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Disabled, true))
                {
                    //
                    // NOTE: If the dictionary of "enabled" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (disabledTraceCategories == null)
                        disabledTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be added to.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Add or modify the trace category.
                            //
                            int oldValue;

                            if (disabledTraceCategories.TryGetValue(
                                    category, out oldValue))
                            {
                                value += oldValue;
                            }

                            disabledTraceCategories[category] = value;
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Penalty, true))
                {
                    //
                    // NOTE: If the dictionary of "penalty" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (penaltyTraceCategories == null)
                        penaltyTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be added to.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Add or modify the trace category.
                            //
                            int oldValue;

                            if (penaltyTraceCategories.TryGetValue(
                                    category, out oldValue))
                            {
                                value += oldValue;
                            }

                            penaltyTraceCategories[category] = value;
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Bonus, true))
                {
                    //
                    // NOTE: If the dictionary of "bonus" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (bonusTraceCategories == null)
                        bonusTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be added to.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Add or modify the trace category.
                            //
                            int oldValue;

                            if (bonusTraceCategories.TryGetValue(
                                    category, out oldValue))
                            {
                                value += oldValue;
                            }

                            bonusTraceCategories[category] = value;
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified categories from the default set of
        /// trace categories.
        /// </summary>
        /// <param name="categories">
        /// The trace categories to remove.  This value may be null.
        /// </param>
        private static void UnsetTraceCategories(
            IEnumerable<string> categories /* in */
            )
        {
            UnsetTraceCategories(TraceCategoryType.Default, categories);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified categories from the trace category
        /// dictionaries of the specified type(s).  This method assumes the
        /// static lock is held.
        /// </summary>
        /// <param name="categoryType">
        /// The type(s) of trace category dictionaries to remove from.
        /// </param>
        /// <param name="categories">
        /// The trace categories to remove.  This value may be null.
        /// </param>
        private static void UnsetTraceCategories(
            TraceCategoryType categoryType, /* in */
            IEnumerable<string> categories  /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Enabled, true))
                {
                    //
                    // NOTE: If the dictionary of "enabled" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (enabledTraceCategories == null)
                        enabledTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be removed from.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Remove the trace category.
                            //
                            enabledTraceCategories.Remove(category);
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Disabled, true))
                {
                    //
                    // NOTE: If the dictionary of "disabled" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (disabledTraceCategories == null)
                        disabledTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be removed from.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Remove the trace category.
                            //
                            disabledTraceCategories.Remove(category);
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Penalty, true))
                {
                    //
                    // NOTE: If the dictionary of "penalty" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (penaltyTraceCategories == null)
                        penaltyTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be removed from.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Remove the trace category.
                            //
                            penaltyTraceCategories.Remove(category);
                        }
                    }
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Bonus, true))
                {
                    //
                    // NOTE: If the dictionary of "bonus" trace categories
                    //       has not been created yet, do so now.
                    //
                    if (bonusTraceCategories == null)
                        bonusTraceCategories = new IntDictionary();

                    //
                    // NOTE: If there are no trace categories specified,
                    //       the trace category dictionary may be created;
                    //       however, it will not be removed from.
                    //
                    if (categories != null)
                    {
                        foreach (string category in categories)
                        {
                            //
                            // NOTE: Skip null categories.
                            //
                            if (category == null)
                                continue;

                            //
                            // NOTE: Remove the trace category.
                            //
                            bonusTraceCategories.Remove(category);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the entries from the default set of trace
        /// category dictionaries, leaving the dictionaries themselves intact.
        /// </summary>
        private static void ClearTraceCategories()
        {
            ClearTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the entries from the trace category dictionaries
        /// of the specified type(s), leaving the dictionaries themselves
        /// intact.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="categoryType">
        /// The type(s) of trace category dictionaries to clear.
        /// </param>
        private static void ClearTraceCategories(
            TraceCategoryType categoryType /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Enabled, true) &&
                    (enabledTraceCategories != null))
                {
                    enabledTraceCategories.Clear();
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Disabled, true) &&
                    (disabledTraceCategories != null))
                {
                    disabledTraceCategories.Clear();
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Penalty, true) &&
                    (penaltyTraceCategories != null))
                {
                    penaltyTraceCategories.Clear();
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Bonus, true) &&
                    (bonusTraceCategories != null))
                {
                    bonusTraceCategories.Clear();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the default set of trace category dictionaries,
        /// clearing and removing them entirely.
        /// </summary>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which trace category
        /// dictionaries, if any, were reset.
        /// </returns>
        private static TraceStateType ResetTraceCategories()
        {
            return ResetTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the trace category dictionaries of the specified
        /// type(s), clearing and removing them entirely.  This method assumes
        /// the static lock is held.
        /// </summary>
        /// <param name="categoryType">
        /// The type(s) of trace category dictionaries to reset.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which trace category
        /// dictionaries, if any, were reset.
        /// </returns>
        private static TraceStateType ResetTraceCategories(
            TraceCategoryType categoryType /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceStateType result = TraceStateType.None;

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Enabled, true) &&
                    (enabledTraceCategories != null))
                {
                    enabledTraceCategories.Clear();
                    enabledTraceCategories = null;

                    result |= TraceStateType.EnabledCategories;
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Disabled, true) &&
                    (disabledTraceCategories != null))
                {
                    disabledTraceCategories.Clear();
                    disabledTraceCategories = null;

                    result |= TraceStateType.DisabledCategories;
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Penalty, true) &&
                    (penaltyTraceCategories != null))
                {
                    penaltyTraceCategories.Clear();
                    penaltyTraceCategories = null;

                    result |= TraceStateType.PenaltyCategories;
                }

                if (FlagOps.HasFlags(
                        categoryType, TraceCategoryType.Bonus, true) &&
                    (bonusTraceCategories != null))
                {
                    bonusTraceCategories.Clear();
                    bonusTraceCategories = null;

                    result |= TraceStateType.BonusCategories;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the trace category dictionaries indicated by
        /// the specified state type from the relevant environment variables
        /// (and/or their defaults), if they have not already been set.
        /// Exceptions are caught and ignored.  This method assumes the static
        /// lock is held.
        /// </summary>
        /// <param name="stateType">
        /// The trace state flags indicating which trace category dictionaries
        /// to initialize, and whether to force (re-)initialization.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to fall back to the default trace categories when the
        /// relevant environment variable is not present.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which trace category
        /// dictionaries, if any, were initialized.
        /// </returns>
        private static TraceStateType InitializeTraceCategories(
            TraceStateType stateType, /* in */
            bool useDefaults          /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                IntDictionary categories; /* REUSED */

                bool force = FlagOps.HasFlags(
                    stateType, TraceStateType.Force, true);

                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.EnabledCategories, true) &&
                    (force || (enabledTraceCategories == null)))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        categories = CheckForTraceCategories(
                            EnvVars.TraceCategories, EnabledName, 1);

                        if (categories != null)
                        {
                            enabledTraceCategories = categories;
                            result |= TraceStateType.EnabledCategories;
                        }
                        else if (useDefaults)
                        {
                            enabledTraceCategories = DefaultTraceCategories;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.DisabledCategories, true) &&
                    (force || (disabledTraceCategories == null)))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        categories = CheckForTraceCategories(
                            EnvVars.NoTraceCategories, DisabledName, 1);

                        if (categories != null)
                        {
                            disabledTraceCategories = categories;
                            result |= TraceStateType.DisabledCategories;
                        }
                        else if (useDefaults)
                        {
                            disabledTraceCategories = DefaultTraceCategories;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.PenaltyCategories, true) &&
                    (force || (penaltyTraceCategories == null)))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        categories = CheckForTraceCategories(
                            EnvVars.PenaltyTraceCategories, PenaltyName, 1);

                        if (categories != null)
                        {
                            penaltyTraceCategories = categories;
                            result |= TraceStateType.PenaltyCategories;
                        }
                        else if (useDefaults)
                        {
                            penaltyTraceCategories = DefaultTraceCategories;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (FlagOps.HasFlags(
                        stateType, TraceStateType.BonusCategories, true) &&
                    (force || (bonusTraceCategories == null)))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        categories = CheckForTraceCategories(
                            EnvVars.BonusTraceCategories, BonusName, 1);

                        if (categories != null)
                        {
                            bonusTraceCategories = categories;
                            result |= TraceStateType.BonusCategories;
                        }
                        else if (useDefaults)
                        {
                            bonusTraceCategories = DefaultTraceCategories;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Format String Management
        /// <summary>
        /// This method gets the explicitly configured trace format string, if
        /// any.  This method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The configured trace format string, or null if none is set.
        /// </returns>
        private static string GetTraceFormatString()
        {
            lock (syncRoot)
            {
                return traceFormatString;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the explicitly configured trace format string,
        /// removing it.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTraceFormatString()
        {
            lock (syncRoot)
            {
                traceFormatString = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the explicitly configured trace format string.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <param name="format">
        /// The new trace format string.  This value may be null.
        /// </param>
        private static void SetTraceFormatString(
            string format /* in */
            )
        {
            lock (syncRoot)
            {
                traceFormatString = format;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Format Index Management
        /// <summary>
        /// This method gets the configured built-in trace format index, if any.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// The configured built-in trace format index, or null if none is set.
        /// </returns>
        private static int? GetTraceFormatIndex()
        {
            lock (syncRoot)
            {
                return traceFormatIndex;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the configured built-in trace format index,
        /// removing it.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTraceFormatIndex()
        {
            lock (syncRoot)
            {
                traceFormatIndex = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the configured built-in trace format index.  This
        /// method assumes the static lock is held.
        /// </summary>
        /// <param name="index">
        /// The new built-in trace format index.  This value may be null.
        /// </param>
        private static void SetTraceFormatIndex(
            int? index /* in */
            )
        {
            lock (syncRoot)
            {
                traceFormatIndex = index;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Format Management
        /// <summary>
        /// This method translates a (possibly negative) built-in trace format
        /// index into an absolute index, where negative indexes are relative to
        /// the end of the set of built-in trace formats.
        /// </summary>
        /// <param name="index">
        /// The built-in trace format index to translate.
        /// </param>
        /// <param name="length">
        /// The total number of built-in trace formats.
        /// </param>
        /// <returns>
        /// The translated, absolute built-in trace format index.
        /// </returns>
        private static int TranslateTraceFormatIndex(
            int index, /* in */
            int length /* in */
            )
        {
            if (index <= Index.Invalid)
                return length - (Index.Invalid - index) - 1;

            return index;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified built-in trace format
        /// index is valid, translating it to an absolute index in the process.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <param name="index">
        /// The built-in trace format index to check; upon success, this value
        /// is set to the translated, absolute index.
        /// </param>
        /// <returns>
        /// True if the specified built-in trace format index is valid;
        /// otherwise, false.
        /// </returns>
        private static bool CheckBuiltInTraceFormatIndex(
            ref int index /* in, out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (TraceFormats != null)
                {
                    int length = TraceFormats.Length;

                    int localIndex = TranslateTraceFormatIndex(
                        index, length);

                    if ((localIndex >= 0) && (localIndex < length))
                    {
                        index = localIndex;
                        return true;
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in trace format string at the specified
        /// index.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="index">
        /// The built-in trace format index.
        /// </param>
        /// <returns>
        /// The built-in trace format string at the specified index, or null if
        /// the index is not valid.
        /// </returns>
        private static string GetBuiltInTraceFormat(
            int index /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((TraceFormats != null) && /* REDUNDANT */
                    CheckBuiltInTraceFormatIndex(ref index))
                {
                    return TraceFormats[index];
                }
                else
                {
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in trace format at the specified index
        /// as the active trace format.  This method assumes the static lock is
        /// held.
        /// </summary>
        /// <param name="index">
        /// The built-in trace format index to select.
        /// </param>
        /// <returns>
        /// True if the built-in trace format was selected; otherwise, false.
        /// </returns>
        private static bool SetBuiltInTraceFormat(
            int index /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((TraceFormats != null) && /* REDUNDANT */
                    CheckBuiltInTraceFormatIndex(ref index))
                {
                    SetTraceFormatIndex(index);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the effective trace format string, preferring the
        /// explicitly configured trace format string, then the configured
        /// built-in trace format index, and finally the fallback trace format
        /// (when its use is enabled).  This method assumes the static lock is
        /// held.
        /// </summary>
        /// <returns>
        /// The effective trace format string, or null if none is available.
        /// </returns>
        private static string GetEffectiveTraceFormat()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (traceFormatString != null)
                    return traceFormatString;

                if (traceFormatIndex != null)
                    return GetBuiltInTraceFormat((int)traceFormatIndex);

                if (GetUseFallbackTraceFormat())
                    return GetFallbackTraceFormat();

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the effective trace format string for a message,
        /// honoring any per-message trace format selection flags present in the
        /// specified trace priority mask and clearing them as they are
        /// processed.
        /// </summary>
        /// <param name="priorities">
        /// The trace priority mask to examine; any trace format selection flags
        /// that are honored are cleared from this value.
        /// </param>
        /// <returns>
        /// The effective trace format string, or null if none is available.
        /// </returns>
        private static string GetEffectiveTraceFormat(
            ref TracePriority priorities /* in, out */
            )
        {
            if (FlagOps.HasFlags(
                    priorities, TracePriority.EnableMinimumFormatFlag,
                    true))
            {
                priorities &= ~TracePriority.EnableMinimumFormatFlag;
                return GetMinimumTraceFormat();
            }

            if (FlagOps.HasFlags(
                    priorities, TracePriority.EnableMediumLowFormatFlag,
                    true))
            {
                priorities &= ~TracePriority.EnableMediumLowFormatFlag;
                return GetMediumLowTraceFormat();
            }

            if (FlagOps.HasFlags(
                    priorities, TracePriority.EnableMediumFormatFlag,
                    true))
            {
                priorities &= ~TracePriority.EnableMediumFormatFlag;
                return GetMediumTraceFormat();
            }

            if (FlagOps.HasFlags(
                    priorities, TracePriority.EnableMediumHighFormatFlag,
                    true))
            {
                priorities &= ~TracePriority.EnableMediumHighFormatFlag;
                return GetMediumHighTraceFormat();
            }

            if (FlagOps.HasFlags(
                    priorities, TracePriority.EnableMaximumFormatFlag,
                    true))
            {
                priorities &= ~TracePriority.EnableMaximumFormatFlag;
                return GetMaximumTraceFormat();
            }

            return GetEffectiveTraceFormat();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the specified value is a valid trace
        /// format, either a known <see cref="TraceFormatType" /> name or a
        /// format string that can be used with the appropriate
        /// <c>String.Format</c> overload without throwing.
        /// </summary>
        /// <param name="value">
        /// The trace format value to verify.
        /// </param>
        /// <param name="formatType">
        /// Upon success, receives the kind of trace format that was recognized.
        /// </param>
        /// <param name="errors">
        /// Upon failure, receives one or more errors describing why the trace
        /// format could not be verified.
        /// </param>
        /// <returns>
        /// True if the specified value is a valid trace format; otherwise,
        /// false.
        /// </returns>
        private static bool VerifyTraceFormat(
            string value,                   /* in */
            ref TraceFormatType formatType, /* out */
            ref ResultList errors           /* out */
            )
        {
            if (String.IsNullOrEmpty(value))
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid trace format");
                return false;
            }

            object enumValue;
            Result error = null;

            enumValue = EnumOps.TryParse(
                typeof(TraceFormatType), value, true, true, ref error);

            if (enumValue is TraceFormatType)
            {
                formatType = (TraceFormatType)enumValue;
                return true;
            }

            //
            // HACK: This code is doing something slightly "clever".
            //       To verify caller provided trace format string,
            //       it creates an empty string array of the number
            //       of replacements used by the tracing subsystem,
            //       then attempts to use the caller provided trace
            //       format string with that array to make sure the
            //       appropriate String.Format method overload does
            //       not throw an exceptions.
            //
            try
            {
                string[] args = new string[FormatParameterCount];

                string formatted = String.Format(
                    value, args); /* throw */

                if (formatted != null)
                {
                    formatType = TraceFormatType.String;
                    return true;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("string formatting failed");
                }
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the active trace format (either a format
        /// string or a built-in format index) from the relevant environment
        /// variable (and/or its default), if it has not already been set.
        /// Exceptions are caught and ignored.  This method assumes the static
        /// lock is held.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re-)initialization even when the trace format has
        /// already been set.
        /// </param>
        /// <param name="useDefaults">
        /// Non-zero to fall back to the default trace format when the relevant
        /// environment variable is not present.
        /// </param>
        /// <returns>
        /// A <see cref="TraceStateType" /> value indicating which portions of
        /// the trace format, if any, were initialized.
        /// </returns>
        private static TraceStateType InitializeTraceFormat(
            bool force,      /* in */
            bool useDefaults /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                TraceStateType result = TraceStateType.None;

                ///////////////////////////////////////////////////////////////

                if (force ||
                    ((traceFormatString == null) && (traceFormatIndex == null)))
                {
                    //
                    // HACK: Since there is nothing we can do about it here,
                    //       and its initialization is non-critical, do not
                    //       let exceptions escape from the method called
                    //       here.  Also, there was the possibility of this
                    //       method causing an issue for Interpreter.Create
                    //       if an exception escaped from this point, per a
                    //       variant of Coverity issue #236095.
                    //
                    try
                    {
                        FormatPair formatPair = CheckForTraceFormat(
                            EnvVars.TraceFormat);

                        if (formatPair != null)
                        {
                            int index = (int)formatPair.X;

                            if (CheckBuiltInTraceFormatIndex(ref index))
                            {
                                traceFormatString = null;
                                traceFormatIndex = index;

                                result |= TraceStateType.FormatIndex;
                            }
                            else
                            {
                                traceFormatString = formatPair.Y;
                                traceFormatIndex = null;

                                result |= TraceStateType.FormatString;
                            }
                        }
                        else if (useDefaults)
                        {
                            traceFormatString = DefaultTraceFormatString;
                            traceFormatIndex = DefaultTraceFormatIndex;
                        }
                    }
                    catch
                    {
                        // do nothing.
                    }
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Built-In Formats
        /// <summary>
        /// This method gets the built-in "bare" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "bare" trace format string.
        /// </returns>
        private static string GetBareTraceFormat()
        {
            return GetBuiltInTraceFormat(1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "bare" trace format as the active
        /// trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "bare" trace format was selected; otherwise,
        /// false.
        /// </returns>
        private static bool SetBareTraceFormat()
        {
            return SetBuiltInTraceFormat(1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "bare" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "bare" trace
        /// format; otherwise, false.
        /// </returns>
        private static bool IsBareTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetBareTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in "minimum" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "minimum" trace format string.
        /// </returns>
        private static string GetMinimumTraceFormat()
        {
            return GetBuiltInTraceFormat(2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "minimum" trace format as the
        /// active trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "minimum" trace format was selected; otherwise,
        /// false.
        /// </returns>
        private static bool SetMinimumTraceFormat()
        {
            return SetBuiltInTraceFormat(2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "minimum" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "minimum" trace
        /// format; otherwise, false.
        /// </returns>
        private static bool IsMinimumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMinimumTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in "medium-low" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "medium-low" trace format string.
        /// </returns>
        private static string GetMediumLowTraceFormat()
        {
            return GetBuiltInTraceFormat(3);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "medium-low" trace format as the
        /// active trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "medium-low" trace format was selected;
        /// otherwise, false.
        /// </returns>
        private static bool SetMediumLowTraceFormat()
        {
            return SetBuiltInTraceFormat(3);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "medium-low" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "medium-low"
        /// trace format; otherwise, false.
        /// </returns>
        private static bool IsMediumLowTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumLowTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in "medium" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "medium" trace format string.
        /// </returns>
        private static string GetMediumTraceFormat()
        {
            return GetBuiltInTraceFormat(4);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "medium" trace format as the active
        /// trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "medium" trace format was selected; otherwise,
        /// false.
        /// </returns>
        private static bool SetMediumTraceFormat()
        {
            return SetBuiltInTraceFormat(4);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "medium" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "medium" trace
        /// format; otherwise, false.
        /// </returns>
        private static bool IsMediumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in "medium-high" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "medium-high" trace format string.
        /// </returns>
        private static string GetMediumHighTraceFormat()
        {
            return GetBuiltInTraceFormat(Index.Invalid - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "medium-high" trace format as the
        /// active trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "medium-high" trace format was selected;
        /// otherwise, false.
        /// </returns>
        private static bool SetMediumHighTraceFormat()
        {
            return SetBuiltInTraceFormat(Index.Invalid - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "medium-high" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "medium-high"
        /// trace format; otherwise, false.
        /// </returns>
        private static bool IsMediumHighTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumHighTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the built-in "maximum" trace format string.
        /// </summary>
        /// <returns>
        /// The built-in "maximum" trace format string.
        /// </returns>
        private static string GetMaximumTraceFormat()
        {
            return GetBuiltInTraceFormat(Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the built-in "maximum" trace format as the
        /// active trace format.
        /// </summary>
        /// <returns>
        /// True if the built-in "maximum" trace format was selected; otherwise,
        /// false.
        /// </returns>
        private static bool SetMaximumTraceFormat()
        {
            return SetBuiltInTraceFormat(Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the effective trace format is the
        /// built-in "maximum" trace format.
        /// </summary>
        /// <returns>
        /// True if the effective trace format is the built-in "maximum" trace
        /// format; otherwise, false.
        /// </returns>
        private static bool IsMaximumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMaximumTraceFormat());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Fallback Format Management
        /// <summary>
        /// This method gets the fallback trace format string, which is used
        /// when no other trace format has been configured.  This method assumes
        /// the static lock is held.
        /// </summary>
        /// <returns>
        /// The fallback trace format string, or null if none is set.
        /// </returns>
        private static string GetFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                return FallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the fallback trace format string back to its
        /// default value.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                FallbackTraceFormat = DefaultFallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Be careful calling this method with a parameter value
        //          of false because that could totally disable all trace
        //          output.
        //
        /// <summary>
        /// This method enables or disables the fallback trace format string, by
        /// setting it to the default trace format or to null, respectively.
        /// This method assumes the static lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to set the fallback trace format to the default trace
        /// format; zero to clear it (which could disable all trace output).
        /// </param>
        private static void SetFallbackTraceFormat(
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                FallbackTraceFormat = enabled ? DefaultTraceFormat : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the fallback trace format should be
        /// used when no other trace format has been configured.  This method
        /// assumes the static lock is held.
        /// </summary>
        /// <returns>
        /// True if the fallback trace format should be used; otherwise, false.
        /// </returns>
        private static bool GetUseFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                return UseFallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the flag that controls whether the fallback trace
        /// format should be used back to its default value.  This method
        /// assumes the static lock is held.
        /// </summary>
        private static void ResetUseFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                UseFallbackTraceFormat = DefaultUseFallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the flag that controls whether the fallback trace
        /// format should be used.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to use the fallback trace format when no other trace format
        /// has been configured; otherwise, zero.
        /// </param>
        private static void SetUseFallbackTraceFormat(
            bool enabled /* in */
            )
        {
            lock (syncRoot)
            {
                UseFallbackTraceFormat = enabled;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Message Helper Methods
        /// <summary>
        /// This method appends the trailing new line placeholder to the
        /// specified trace format string.
        /// </summary>
        /// <param name="traceFormat">
        /// The trace format string to modify; upon return, it includes the
        /// trailing new line placeholder.
        /// </param>
        private static void MaybeAddNewLines(
            ref string traceFormat /* in, out */
            )
        {
            traceFormat = String.Format(
                "{0}{1}", traceFormat, TraceNewLineFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the current values of the individual trace format
        /// flag fields.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="zero">
        /// Receives the flag indicating whether the date and time are included
        /// in the trace output.
        /// </param>
        /// <param name="one">
        /// Receives the flag indicating whether the trace priority is included
        /// in the trace output.
        /// </param>
        /// <param name="two">
        /// Receives the flag indicating whether the server name is included in
        /// the trace output.
        /// </param>
        /// <param name="three">
        /// Receives the flag indicating whether the test name is included in
        /// the trace output.
        /// </param>
        /// <param name="four">
        /// Receives the flag indicating whether the application domain is
        /// included in the trace output.
        /// </param>
        /// <param name="five">
        /// Receives the flag indicating whether the interpreter is included in
        /// the trace output.
        /// </param>
        /// <param name="six">
        /// Receives the flag indicating whether the thread identifier is
        /// included in the trace output.
        /// </param>
        /// <param name="seven">
        /// Receives the flag indicating whether the method name is included in
        /// the trace output.
        /// </param>
        /// <param name="eight">
        /// Receives the flag indicating whether the stack trace is included in
        /// the trace output.
        /// </param>
        /// <param name="nine">
        /// Receives the flag indicating whether extra new lines are included in
        /// the trace output.
        /// </param>
        private static void GetTraceFormatFlags(
            out bool zero,  /* out */
            out bool one,   /* out */
            out bool two,   /* out */
            out bool three, /* out */
            out bool four,  /* out */
            out bool five,  /* out */
            out bool six,   /* out */
            out bool seven, /* out */
            out bool eight, /* out */
            out bool nine   /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                zero = traceDateTime;
                one = tracePriority;
                two = traceServerName;
                three = traceTestName;
                four = traceAppDomain;
                five = traceInterpreter;
                six = traceThreadId;
                seven = traceMethod;
                eight = traceStack;
                nine = traceExtraNewLines;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the individual trace format flag fields to the
        /// specified values.  This method assumes the static lock is held.
        /// </summary>
        /// <param name="zero">
        /// The flag indicating whether the date and time are included in the
        /// trace output.
        /// </param>
        /// <param name="one">
        /// The flag indicating whether the trace priority is included in the
        /// trace output.
        /// </param>
        /// <param name="two">
        /// The flag indicating whether the server name is included in the trace
        /// output.
        /// </param>
        /// <param name="three">
        /// The flag indicating whether the test name is included in the trace
        /// output.
        /// </param>
        /// <param name="four">
        /// The flag indicating whether the application domain is included in
        /// the trace output.
        /// </param>
        /// <param name="five">
        /// The flag indicating whether the interpreter is included in the trace
        /// output.
        /// </param>
        /// <param name="six">
        /// The flag indicating whether the thread identifier is included in the
        /// trace output.
        /// </param>
        /// <param name="seven">
        /// The flag indicating whether the method name is included in the trace
        /// output.
        /// </param>
        /// <param name="eight">
        /// The flag indicating whether the stack trace is included in the trace
        /// output.
        /// </param>
        /// <param name="nine">
        /// The flag indicating whether extra new lines are included in the
        /// trace output.
        /// </param>
        private static void SetTraceFormatFlags(
            bool zero,  /* in */
            bool one,   /* in */
            bool two,   /* in */
            bool three, /* in */
            bool four,  /* in */
            bool five,  /* in */
            bool six,   /* in */
            bool seven, /* in */
            bool eight, /* in */
            bool nine   /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                traceDateTime = zero;
                tracePriority = one;
                traceServerName = two;
                traceTestName = three;
                traceAppDomain = four;
                traceInterpreter = five;
                traceThreadId = six;
                traceMethod = seven;
                traceStack = eight;
                traceExtraNewLines = nine;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the individual trace format flag fields back to
        /// their default values.  This method assumes the static lock is held.
        /// </summary>
        private static void ResetTraceFormatFlags()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // TODO: Good defaults?
                //
                traceDateTime = false;
                tracePriority = true;
                traceServerName = true;
                traceTestName = true;
                traceAppDomain = false;
                traceInterpreter = false;
                traceThreadId = true;
                traceMethod = false;
                traceStack = false;
                traceExtraNewLines = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables the individual trace format flag
        /// fields together, optionally including the more verbose ones (the
        /// stack trace and extra new lines).  This method assumes the static
        /// lock is held.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to enable the affected trace format flags; zero to disable
        /// them.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to also affect the more verbose trace format flags (the
        /// stack trace and extra new lines).
        /// </param>
        private static void EnableTraceFormatFlags(
            bool enabled, /* in */
            bool verbose  /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                traceDateTime = enabled;
                tracePriority = enabled;
                traceServerName = enabled;
                traceTestName = enabled;
                traceAppDomain = enabled;
                traceInterpreter = enabled;
                traceThreadId = enabled;
                traceMethod = enabled;

                if (verbose)
                {
                    traceStack = enabled;
                    traceExtraNewLines = enabled;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the effective trace format flags for a message,
        /// starting from the current trace format flag fields and then applying
        /// any per-message trace format flag adjustments present in the
        /// specified trace priority mask (which are cleared as they are
        /// processed).
        /// </summary>
        /// <param name="priorities">
        /// The trace priority mask to examine; any trace format flag
        /// adjustments that are applied are cleared from this value.
        /// </param>
        /// <param name="zero">
        /// Receives the flag indicating whether the date and time are included
        /// in the trace output.
        /// </param>
        /// <param name="one">
        /// Receives the flag indicating whether the trace priority is included
        /// in the trace output.
        /// </param>
        /// <param name="two">
        /// Receives the flag indicating whether the server name is included in
        /// the trace output.
        /// </param>
        /// <param name="three">
        /// Receives the flag indicating whether the test name is included in
        /// the trace output.
        /// </param>
        /// <param name="four">
        /// Receives the flag indicating whether the application domain is
        /// included in the trace output.
        /// </param>
        /// <param name="five">
        /// Receives the flag indicating whether the interpreter is included in
        /// the trace output.
        /// </param>
        /// <param name="six">
        /// Receives the flag indicating whether the thread identifier is
        /// included in the trace output.
        /// </param>
        /// <param name="seven">
        /// Receives the flag indicating whether the method name is included in
        /// the trace output.
        /// </param>
        /// <param name="eight">
        /// Receives the flag indicating whether the stack trace is included in
        /// the trace output.
        /// </param>
        /// <param name="nine">
        /// Receives the flag indicating whether extra new lines are included in
        /// the trace output.
        /// </param>
        private static void GetTraceFormatFlags(
            ref TracePriority priorities, /* in, out */
            out bool zero,                /* out */
            out bool one,                 /* out */
            out bool two,                 /* out */
            out bool three,               /* out */
            out bool four,                /* out */
            out bool five,                /* out */
            out bool six,                 /* out */
            out bool seven,               /* out */
            out bool eight,               /* out */
            out bool nine                 /* out */
            )
        {
            GetTraceFormatFlags(
                out zero, out one, out two, out three,
                out four, out five, out six, out seven,
                out eight, out nine);

            TracePrioritiesToFormatFlags(
                ref priorities, ref zero, ref one, ref two,
                ref three, ref four, ref five, ref six,
                ref seven, ref eight, ref nine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Message Methods
        #region Private
        /// <summary>
        /// This method records statistics indicating that a trace message was
        /// emitted in the context of a lock warning or error, based on the
        /// specified trace priority flags.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TraceWasForLock(
            TracePriority priority /* in */
            )
        {
            if (FlagOps.HasFlags(priority, TracePriority.Warning, true))
            {
                /* IGNORED */
                Interlocked.Increment(
                    ref traceLockWarnings); /* BREAKPOINT HERE */
            }

            if (FlagOps.HasFlags(priority, TracePriority.Error, true))
            {
                /* IGNORED */
                Interlocked.Increment(
                    ref traceLockErrors); /* BREAKPOINT HERE */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Debug Write Core
        /// <summary>
        /// This method is the core implementation used to write a raw value
        /// (i.e. one that is not subject to the normal trace category and
        /// priority checks) to the trace output, honoring the configured trace
        /// format and listeners.  It is guarded against unbounded reentrancy.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the value, if any.  This value may
        /// be null.
        /// </param>
        /// <param name="value">
        /// The value to write to the trace output.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when it would
        /// otherwise be suppressed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        // [Conditional("DEBUG_TRACE")] // HACK: Always included.
        private static void DebugWriteToCore(
            Interpreter interpreter, /* in */
            string value,            /* in */
            bool force               /* in */
            )
        {
            int levels = Interlocked.Increment(ref writeLevels);

            try
            {
                if (levels <= DefaultMaximumWriteLevels)
                {
                    if (!IsTracePossible()) /* EXEMPT */
                        return;

                    if (!IsWritePossible())
                        return;

                    /* IGNORED */
                    TracePrioritiesToFormatString();

                    string traceFormat = GetEffectiveTraceFormat();

                    if (traceFormat == null)
                        return;

                    /* IGNORED */
                    TracePrioritiesToFormatFlags();

                    bool traceDateTime;
                    bool tracePriority;
                    bool traceServerName;
                    bool traceTestName;
                    bool traceAppDomain;
                    bool traceInterpreter;
                    bool traceThreadId;
                    bool traceMethod;
                    bool traceStack;
                    bool traceExtraNewLines;

                    GetTraceFormatFlags(
                        out traceDateTime, out tracePriority,
                        out traceServerName, out traceTestName,
                        out traceAppDomain, out traceInterpreter,
                        out traceThreadId, out traceMethod,
                        out traceStack, out traceExtraNewLines);

                    if (traceExtraNewLines)
                        MaybeAddNewLines(ref traceFormat);

                    bool nested = (levels > 1);

                    DebugOps.WriteTo(
                        interpreter, FormatOps.TraceOutput(
                        traceFormat, nested ? TraceNestedIndicator : null,
                        traceDateTime ? (DateTime?)TimeOps.GetNow() : null,
                        null,
#if WEB && !NET_STANDARD_20
                        traceServerName ? PathOps.GetServerName() : null,
#endif
                        traceTestName ? TestOps.GetCurrentName(interpreter) : null,
                        traceAppDomain ? AppDomainOps.GetCurrent() : null,
                        traceInterpreter ? interpreter : null, traceThreadId ?
                        (int?)GlobalState.GetCurrentSystemThreadId() : null,
                        value, traceMethod, traceStack, 1), force);
                }
                else
                {
                    DebugOps.MaybeBreak();
                }
            }
#if NATIVE
            catch (Exception e)
#else
            catch
#endif
            {
                Interlocked.Increment(ref traceException);

#if NATIVE
                DebugOps.Output(e, DebugPriority.FromTraceException);
#endif
            }
            finally
            {
                Interlocked.Decrement(ref writeLevels);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Debug Trace Core
        /// <summary>
        /// This method is the core implementation used to write an
        /// already-formatted trace message to the active interpreter host (when
        /// enabled and available), the configured trace listeners, and/or the
        /// log, after applying the trace category and priority checks (unless
        /// they are skipped).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="message">
        /// The already-formatted trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message, if any.
        /// </param>
        /// <param name="methodName">
        /// The name of the method that originated the trace message, if any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipChecks">
        /// Non-zero to skip the trace category and priority checks, forcing the
        /// message to be written.
        /// </param>
        /// <returns>
        /// True if the trace message was written; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        // [Conditional("DEBUG_TRACE")] // HACK: Must return boolean.
        private static bool DebugTraceRaw(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            string methodName,       /* in */
            TracePriority priority,  /* in */
            bool skipChecks          /* in */
            )
        {
            //
            // TODO: Redirect these writes to the active IHost, if any?  On
            //       second thought, that is probably a bad idea.  Instead,
            //       these writes can be captured into a [file] stream using
            //       the TraceTextWriter property of the active interpreter.
            //
            if (CanSkipChecks(methodName, skipChecks) ||
                IsTraceEnabled(priority, category, methodName))
            {
                if (interpreter != null)
                {
                    //
                    // NOTE: If the trace-to-host flag is non-zero -AND-
                    //       a valid interpreter host is available, use
                    //       that to write the trace message; otherwise,
                    //       fallback to the previous default handling.
                    //
                    if (GetTraceToInterpreterHost())
                    {
                        if (Interlocked.Increment(
                                ref traceToInterpreterHostLevels) == 1)
                        {
                            try
                            {
                                IInteractiveHost interactiveHost =
                                    interpreter.GetInteractiveHost();

                                if (interactiveHost != null)
                                {
                                    if (category != null)
                                    {
                                        return interactiveHost.Write(
                                            String.Format(
                                                TraceListenerFormat,
                                            category, message)); /* throw */
                                    }
                                    else
                                    {
                                        return interactiveHost.Write(
                                            message); /* throw */
                                    }
                                }
                            }
                            finally
                            {
                                Interlocked.Decrement(
                                    ref traceToInterpreterHostLevels);
                            }
                        }
                        else
                        {
                            Interlocked.Decrement(
                                ref traceToInterpreterHostLevels);
                        }
                    }

                    //
                    // NOTE: This should return non-zero if the called
                    //       method makes use of the "Trace.Listeners".
                    //       Our caller may rely on this result to know
                    //       when the IBufferedTraceListener instances,
                    //       if any, should be flushed.
                    //
                    if (DebugOps.TraceWrite(
                            interpreter, message, category)) /* throw */
                    {
                        Interlocked.Increment(ref traceWritten);
                        return true;
                    }
                }
                else
                {
                    //
                    // HACK: Disallow displaying categories that have
                    //       non-alphanumeric characters.  That probably
                    //       means it is from an obfuscated assembly and
                    //       there is not much point in cluttering trace
                    //       output with them.
                    //
                    if (!CanDisplayCategory(category))
                        category = null;

                    /* NO RESULT */
                    DebugOps.TraceWrite(
                        message, category); /* EXEMPT */ /* throw */

                    //
                    // NOTE: Return non-zero because the method called
                    //       always makes use of the "Trace.Listeners".
                    //       Our caller may rely on this result to know
                    //       when the IBufferedTraceListener instances,
                    //       if any, should be flushed.
                    //
                    Interlocked.Increment(ref traceWritten);
                    return true;
                }
            }

            TraceWasDropped(interpreter, message, category, priority);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the core implementation used to format and write a
        /// trace message, applying the trace possible, trace enabled, and trace
        /// filter checks (unless they are skipped) and gathering the various
        /// pieces of contextual information (such as the date and time, thread
        /// identifier, method name, and stack trace) selected by the active
        /// trace format flags.  It is guarded against unbounded reentrancy.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that originated the trace message, if
        /// known.  This value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to format and write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message, if any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of call frames to skip when capturing a stack trace, if
        /// any.
        /// </param>
        /// <param name="skipChecks">
        /// Non-zero to skip the trace possible and trace enabled checks.
        /// </param>
        /// <param name="skipFilter">
        /// Non-zero to skip the trace filter check.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        // [Conditional("DEBUG_TRACE")] // HACK: Always included.
        private static void DebugTraceCore(
            Interpreter interpreter, /* in */
            long? threadId,          /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority priority,  /* in */
            int skipFrames,          /* in */
            bool skipChecks,         /* in */
            bool skipFilter          /* in */
            )
        {
            int levels = Interlocked.Increment(ref traceLevels);

            try
            {
                if (levels <= DefaultMaximumTraceLevels)
                {
                    if (!skipChecks && !IsTracePossible())
                    {
                        TraceWasDropped(
                            interpreter, message, category, priority);

                        Interlocked.Increment(ref traceImpossible);
                        return;
                    }

                    if (!skipChecks && !IsTraceEnabled(priority, category))
                    {
                        TraceWasDropped(
                            interpreter, message, category, priority);

                        Interlocked.Increment(ref traceDisabled);
                        return;
                    }

                    if (!skipFilter && IsTraceFiltered(
                            interpreter, ref message, ref category,
                            ref priority))
                    {
                        TraceWasDropped(
                            interpreter, message, category, priority);

                        Interlocked.Increment(ref traceFiltered);
                        return;
                    }

                    /* IGNORED */
                    TracePrioritiesToFormatString();

                    string traceFormat = GetEffectiveTraceFormat(ref priority);

                    if (traceFormat == null)
                    {
                        TraceWasDropped(
                            interpreter, message, category, priority);

                        return;
                    }

                    /* IGNORED */
                    TracePrioritiesToFormatFlags();

                    bool traceDateTime;
                    bool tracePriority;
                    bool traceServerName;
                    bool traceTestName;
                    bool traceAppDomain;
                    bool traceInterpreter;
                    bool traceThreadId;
                    bool traceMethod;
                    bool traceStack;
                    bool traceExtraNewLines;

                    GetTraceFormatFlags(ref priority,
                        out traceDateTime, out tracePriority,
                        out traceServerName, out traceTestName,
                        out traceAppDomain, out traceInterpreter,
                        out traceThreadId, out traceMethod,
                        out traceStack, out traceExtraNewLines);

                    if (traceExtraNewLines)
                        MaybeAddNewLines(ref traceFormat);

                    bool forceFlush = FlagOps.HasFlags(
                        priority, TracePriority.ForceFlush, true);

                    bool nested = (levels > 1);
                    string methodName = null;

#if TEST
                    bool flushBufferedTraceListeners =
#else
                    /* IGNORED */
#endif
                    DebugTraceRaw(interpreter,
                        FormatOps.TraceOutput(
                            traceFormat, nested ? TraceNestedIndicator : null,
                            traceDateTime ? (DateTime?)TimeOps.GetNow() : null,
                            tracePriority ? (TracePriority?)priority : null,
#if WEB && !NET_STANDARD_20
                            traceServerName ? PathOps.GetServerName() : null,
#endif
                            traceTestName ?
                                TestOps.GetCurrentName(interpreter) : null,
                            traceAppDomain ? AppDomainOps.GetCurrent() : null,
                            traceInterpreter ? interpreter : null,
                            traceThreadId ? threadId : null, message,
                            traceMethod, traceStack, skipFrames + 1,
                            ref category, ref methodName),
                        category, methodName, priority, skipChecks); /* throw */

#if TEST
                    //
                    // HACK: If manually requested -OR- necessary, flush *ALL*
                    //       IBufferedTraceListener compatible trace listeners
                    //       that may be present.  Currently, since these are
                    //       not considered to be "production ready", they are
                    //       gated behind the TEST #ifdef (i.e. they are only
                    //       included if the core library is compiled with the
                    //       TEST option defined, which it will be by default).
                    //
                    if (flushBufferedTraceListeners || forceFlush)
                    {
                        /* IGNORED */
                        DebugOps.FlushBufferedTraceListeners(false);
                    }
#endif

                    //
                    // NOTE: This check will also force all trace listeners,
                    //       including IBufferedTraceListener compatible ones,
                    //       to be flushed if the caller has manually set the
                    //       corresponding trace priority flag.
                    //
                    if (forceFlush)
                    {
                        /* NO RESULT */
                        DebugOps.Flush();
                    }
                }
                else
                {
                    DebugOps.MaybeBreak();
                }
            }
#if NATIVE
            catch (Exception e)
#else
            catch
#endif
            {
                Interlocked.Increment(ref traceException);

#if NATIVE
                DebugOps.Output(e, DebugPriority.FromTraceException);
#endif
            }
            finally
            {
                Interlocked.Decrement(ref traceLevels);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public
        #region Statistics Tracking Methods
        /// <summary>
        /// This method records statistics indicating that a trace message was
        /// dropped (i.e. not written) for some reason.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the dropped trace message, if any.
        /// This value may be null.
        /// </param>
        /// <param name="message">
        /// The dropped trace message, if any.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the dropped trace message, if
        /// any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the dropped trace message,
        /// if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TraceWasDropped(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority? priority  /* in */
            )
        {
            Interlocked.Increment(ref traceDropped); /* BREAKPOINT HERE */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records statistics indicating that a trace message was
        /// written to the log.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the logged trace message, if any.
        /// This value may be null.
        /// </param>
        /// <param name="message">
        /// The logged trace message, if any.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the logged trace message, if any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the logged trace message,
        /// if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TraceWasLogged(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority? priority  /* in */
            )
        {
            Interlocked.Increment(ref traceLogged); /* BREAKPOINT HERE */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conditional Debug Trace
        /// <summary>
        /// This method conditionally (when the DEBUG_WRITE compile-time symbol
        /// is defined) writes a raw value to the trace output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the value, if any.  This value may
        /// be null.
        /// </param>
        /// <param name="value">
        /// The value to write to the trace output.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when it would
        /// otherwise be suppressed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_WRITE")]
        public static void DebugWriteTo(
            Interpreter interpreter, /* in */
            string value,            /* in */
            bool force               /* in */
            )
        {
            DebugWriteToCore(interpreter, value, force);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unconditional Debug Trace
        /// <summary>
        /// This method unconditionally writes a raw value to the trace output.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the value, if any.  This value may
        /// be null.
        /// </param>
        /// <param name="value">
        /// The value to write to the trace output.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the value to be written even when it would
        /// otherwise be suppressed.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugWriteToAlways(
            Interpreter interpreter, /* in */
            string value,            /* in */
            bool force               /* in */
            )
        {
            DebugWriteToCore(interpreter, value, force);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conditional Debug Trace
        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message indicating that a lock could not
        /// be acquired, and records the associated lock warning or error
        /// statistics.
        /// </summary>
        /// <param name="method">
        /// The name of the method that was unable to acquire the lock.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="static">
        /// Non-zero if the lock that could not be acquired is a static lock.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that currently holds the lock, if
        /// known.  This value may be null.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void LockTrace(
            string method,          /* in */
            string category,        /* in */
            bool @static,           /* in */
            TracePriority priority, /* in */
            long? threadId          /* in */
            )
        {
            TraceWasForLock(priority);

            DebugTraceAlways(String.Format(
                "{0}: unable to acquire {1}lock: held by thread {2}",
                method, @static ? "static " : String.Empty,
                FormatOps.MaybeNull(threadId)), category, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message indicating that a lock could not
        /// be acquired, including an additional descriptive suffix, and records
        /// the associated lock warning or error statistics.
        /// </summary>
        /// <param name="method">
        /// The name of the method that was unable to acquire the lock.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="suffix">
        /// An additional descriptive suffix to include in the trace message.
        /// </param>
        /// <param name="static">
        /// Non-zero if the lock that could not be acquired is a static lock.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that currently holds the lock, if
        /// known.  This value may be null.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void LockTrace(
            string method,          /* in */
            string category,        /* in */
            string suffix,          /* in */
            bool @static,           /* in */
            TracePriority priority, /* in */
            long? threadId          /* in */
            )
        {
            TraceWasForLock(priority);

            DebugTraceAlways(String.Format(
                "{0}: unable to acquire {1}lock{2}: held by thread {3}",
                method, @static ? "static " : String.Empty, suffix,
                FormatOps.MaybeNull(threadId)), category, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message describing the specified
        /// exception.
        /// </summary>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Exception exception,   /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            DebugTraceAlways(exception, category,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message describing the specified
        /// exception, attributed to the specified thread.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to, if
        /// known.  This value may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            long? threadId,        /* in */
            Exception exception,   /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            DebugTraceAlways(threadId, exception, category,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message describing the specified
        /// exception, prefixed with a label and a list of arguments.
        /// </summary>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="prefix">
        /// A descriptive prefix to include before the argument list in the
        /// trace message.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to include in the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Exception exception,    /* in */
            string category,        /* in */
            string prefix,          /* in */
            ArgumentList arguments, /* in */
            TracePriority priority  /* in */
            )
        {
            DebugTraceAlways(
                exception, category, prefix, arguments,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message describing the specified
        /// exception, associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a stack
        /// trace, if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Interpreter interpreter, /* in */
            Exception exception,     /* in */
            string category,         /* in */
            TracePriority priority,  /* in */
            int skipFrames           /* in */
            )
        {
            DebugTraceAlways(interpreter, exception, category,
                priority | TracePriority.ExtraSkipFrame, skipFrames + 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes the specified trace message.
        /// </summary>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            DebugTraceAlways(message, category,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes the specified trace message, attributed to the
        /// specified thread.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to, if
        /// known.  This value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            long? threadId,        /* in */
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            DebugTraceAlways(threadId, message, category,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes the specified trace message, associated with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a stack
        /// trace, if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority priority,  /* in */
            int skipFrames           /* in */
            )
        {
            DebugTraceAlways(
                interpreter, message, category,
                priority | TracePriority.ExtraSkipFrame, skipFrames + 1);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unconditional Debug Trace
        /// <summary>
        /// This method unconditionally writes a trace message describing the
        /// specified exception, applying the trace possible and trace enabled
        /// checks.
        /// </summary>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            Exception exception,   /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            int skipFrames = 1;

            MaybeAdjustSkipFrames(priority, ref skipFrames);

            string message = FormatOps.TraceException(exception, priority);

#if MAYBE_TRACE
            try
            {
                if (TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        null, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(Interpreter.GetActive(),
                    GlobalState.GetCurrentSystemThreadId(),
                    message, category, priority, skipFrames,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes a trace message describing the
        /// specified exception, attributed to the specified thread, applying
        /// the trace possible and trace enabled checks.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to, if
        /// known.  This value may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DebugTraceAlways(
            long? threadId,        /* in */
            Exception exception,   /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            int skipFrames = 1;

            MaybeAdjustSkipFrames(priority, ref skipFrames);

            string message = FormatOps.TraceException(exception, priority);

#if MAYBE_TRACE
            try
            {
                if (TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        null, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(Interpreter.GetActive(),
                    threadId, message, category, priority, skipFrames,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes a trace message describing the
        /// specified exception, prefixed with a label and an optional list of
        /// script arguments, applying the trace possible and trace enabled
        /// checks.
        /// </summary>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="prefix">
        /// A descriptive prefix to include before the exception in the trace
        /// message.
        /// </param>
        /// <param name="arguments">
        /// The list of script arguments to include in the trace message, if
        /// any.  This value may be null.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DebugTraceAlways(
            Exception exception,    /* in */
            string category,        /* in */
            string prefix,          /* in */
            ArgumentList arguments, /* in */
            TracePriority priority  /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    null, null, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            int skipFrames = 1;

            MaybeAdjustSkipFrames(priority, ref skipFrames);

            string message = FormatOps.TraceException(exception, priority);

#if MAYBE_TRACE
            try
            {
                if (TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        null, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                string formatted = String.Format(
                    (arguments != null) ?
                        "{0}{1}{2}[[SCRIPT ARGUMENTS: {3}]]{2}" : "{0}{1}",
                    prefix, message, Environment.NewLine, arguments);

                DebugTraceCore(Interpreter.GetActive(),
                    GlobalState.GetCurrentSystemThreadId(),
                    formatted, category, priority, skipFrames,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes a trace message describing the
        /// specified exception, associated with the specified interpreter,
        /// applying the trace possible and trace enabled checks.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="exception">
        /// The exception to describe in the trace message.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a stack
        /// trace, if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            Interpreter interpreter, /* in */
            Exception exception,     /* in */
            string category,         /* in */
            TracePriority priority,  /* in */
            int skipFrames           /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    interpreter, null, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    interpreter, null, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            MaybeAdjustSkipFrames(priority, ref skipFrames);

            string message = FormatOps.TraceException(exception, priority);

#if MAYBE_TRACE
            try
            {
                if (TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        interpreter, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(interpreter,
                    GlobalState.GetCurrentSystemThreadId(),
                    message, category, priority, skipFrames + 1,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes the specified trace message,
        /// applying the trace possible and trace enabled checks.
        /// </summary>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    null, message, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    null, message, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            int skipFrames = 1;

            MaybeAdjustSkipFrames(priority, ref skipFrames);

#if MAYBE_TRACE
            try
            {
                if (!FlagOps.HasFlags(
                        priority, TracePriority.NoLimits, true) &&
                    TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        null, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(Interpreter.GetActive(),
                    GlobalState.GetCurrentSystemThreadId(),
                    message, category, priority, skipFrames,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes the specified trace message,
        /// attributed to the specified thread, applying the trace possible and
        /// trace enabled checks.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to attribute the trace message to, if
        /// known.  This value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            long? threadId,        /* in */
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    null, message, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    null, message, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            int skipFrames = 1;

            MaybeAdjustSkipFrames(priority, ref skipFrames);

#if MAYBE_TRACE
            try
            {
                if (!FlagOps.HasFlags(
                        priority, TracePriority.NoLimits, true) &&
                    TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        null, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(Interpreter.GetActive(),
                    threadId, message, category, priority, skipFrames,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes the specified trace message,
        /// associated with the specified interpreter, applying the trace
        /// possible and trace enabled checks.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the trace message, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="message">
        /// The trace message to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a stack
        /// trace, if any.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority priority,  /* in */
            int skipFrames           /* in */
            )
        {
            if (!IsTracePossible())
            {
                TraceWasDropped(
                    interpreter, message, category, priority);

                Interlocked.Increment(ref traceImpossible);
                return;
            }

            if (!IsTraceEnabled(priority, category)) /* HACK: *PERF* Bail. */
            {
                TraceWasDropped(
                    interpreter, message, category, priority);

                Interlocked.Increment(ref traceDisabled);
                return;
            }

            MaybeAdjustSkipFrames(priority, ref skipFrames);

#if MAYBE_TRACE
            try
            {
                if (TraceLimits.IsTripped(message, category, priority))
                {
                    TraceWasDropped(
                        interpreter, message, category, priority);

                    Interlocked.Increment(ref traceTripped);
                    return;
                }
#endif

                DebugTraceCore(interpreter,
                    GlobalState.GetCurrentSystemThreadId(),
                    message, category, priority, skipFrames + 1,
                    true, false);
#if MAYBE_TRACE
            }
            finally
            {
                /* IGNORED */
                TraceLimits.KeepTrack(message, category, priority);
            }
#endif
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Parameter Methods
        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) appends a formatted representation of the specified
        /// name/value parameter pairs to the specified string builder, honoring
        /// the formatting options indicated by the specified trace priority
        /// flags.
        /// </summary>
        /// <param name="priority">
        /// The trace priority flags that control how the parameters are
        /// formatted.
        /// </param>
        /// <param name="builder">
        /// The string builder to which the formatted parameters are appended.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to format.  Its
        /// length must be even.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis; this
        /// may be overridden by the specified trace priority flags.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        private static void AppendTraceParameters(
            TracePriority priority, /* in */
            StringBuilder builder,  /* in, out */
            object[] parameters,    /* in */
            bool ellipsis           /* in */
            )
        {
            if ((builder == null) || (parameters == null))
                return;

            int length = parameters.Length;

            if ((length % 2) != 0)
            {
                DebugTrace(String.Format(
                    "AppendTraceParameters: bad parameters array length {0}",
                    length), typeof(TraceOps).Name, TracePriority.PolicyError);

                return;
            }

            for (int index = 0; index < length; index += 2)
            {
                object parameterName = parameters[index];

                if (!(parameterName is string))
                {
                    DebugTrace(String.Format(
                        "AppendTraceParameters: bad parameter name {0} " +
                        "type {1}, must be {2}", index, FormatOps.TypeName(
                        parameterName), FormatOps.TypeName(typeof(string))),
                        typeof(TraceOps).Name, TracePriority.PolicyError);

                    return;
                }
            }

            if (!ellipsis && FlagOps.HasFlags(
                    priority, TracePriority.UseEllipsis, true))
            {
                ellipsis = true;
            }

            if (ellipsis && FlagOps.HasFlags(
                    priority, TracePriority.NoEllipsis, true))
            {
                ellipsis = false;
            }

            if (FlagOps.HasFlags(
                    priority, TracePriority.SimpleFormatting, true))
            {
                for (int index = 0; index < length; index += 2)
                {
                    object parameterValue = parameters[index + 1];
                    string formattedValue;

                    if (parameterValue is byte[])
                    {
                        //
                        // HACK: Needed by the CheckPolicies method.
                        //
                        formattedValue = ArrayOps.ToHexadecimalString(
                            (byte[])parameterValue);
                    }
                    else if (parameterValue != null)
                    {
                        formattedValue = parameterValue.ToString();
                    }
                    else
                    {
                        continue;
                    }

                    builder.AppendLine();
                    builder.Append(Characters.HorizontalTab);

                    if (ellipsis)
                        formattedValue = FormatOps.Ellipsis(formattedValue);

                    builder.Append(formattedValue);
                }
            }
            else
            {
                for (int index = 0; index < length; index += 2)
                {
                    if (index > 0)
                    {
                        builder.Append(Characters.Comma);
                        builder.Append(Characters.Space);
                    }

                    object parameterName = parameters[index]; /* string? */
                    string formattedName;

                    if (parameterName is string)
                    {
                        formattedName = FormatOps.DisplayValue(
                            (string)parameterName);
                    }
                    else
                    {
                        formattedName = FormatOps.DisplayObject;
                    }

                    if (ellipsis)
                        formattedName = FormatOps.Ellipsis(formattedName);

                    object parameterValue = parameters[index + 1];
                    string formattedValue;

                    if (parameterValue is byte[])
                    {
                        //
                        // HACK: Needed by the CheckPolicies method.
                        //
                        formattedValue = ArrayOps.ToHexadecimalString(
                            (byte[])parameterValue);

                        if (ellipsis)
                        {
                            formattedValue = FormatOps.Ellipsis(
                                formattedValue);
                        }
                    }
                    else
                    {
                        formattedValue = FormatOps.WrapOrNull(
                            true, ellipsis, parameterValue);
                    }

                    builder.AppendFormat(
                        "{0} = {1}", formattedName, formattedValue);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally (when the DEBUG_TRACE compile-time symbol
        /// is defined) writes a trace message describing the specified method,
        /// message, and name/value parameter pairs.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method that originated the trace message, if any.
        /// </param>
        /// <param name="message">
        /// The trace message to write, if any.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message, if any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to include in
        /// the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            string methodName,         /* in */
            string message,            /* in */
            string category,           /* in */
            TracePriority priority,    /* in */
            bool ellipsis,             /* in */
            params object[] parameters /* in */
            )
        {
            DebugTraceAlways(
                methodName, message, category, priority, 1, ellipsis,
                parameters);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unconditionally writes a trace message describing the
        /// specified method, message, and name/value parameter pairs.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method that originated the trace message, if any.
        /// </param>
        /// <param name="message">
        /// The trace message to write, if any.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the trace message, if any.
        /// </param>
        /// <param name="priority">
        /// The trace priority flags associated with the trace message.
        /// </param>
        /// <param name="skipFrames">
        /// The number of additional call frames to skip when capturing a stack
        /// trace, if any.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to include in
        /// the trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            string methodName,         /* in */
            string message,            /* in */
            string category,           /* in */
            TracePriority priority,    /* in */
            int skipFrames,            /* in */
            bool ellipsis,             /* in */
            params object[] parameters /* in */
            )
        {
            StringBuilder builder = StringBuilderFactory.Create();

            AppendTraceParameters(
                priority, builder, parameters, ellipsis);

            if (builder.Length > 0)
            {
                string localMethodName;

                if (!String.IsNullOrEmpty(methodName))
                    localMethodName = methodName;
                else
                    localMethodName = "DebugTrace";

                string localMessage;

                if (!String.IsNullOrEmpty(message))
                    localMessage = message;
                else
                    localMessage = FormatOps.DisplayNoMessage;

                string localCategory;

                if (!String.IsNullOrEmpty(category))
                    localCategory = category;
                else
                    localCategory = FormatOps.DisplayNoCategory;

                DebugTraceAlways(
                    null, String.Format("{0}: {1}, {2}",
                    localMethodName, localMessage, builder),
                    localCategory, priority, skipFrames + 1);
            }

            StringBuilderCache.Release(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Tracing Methods
#if POLICY_TRACE
        /// <summary>
        /// This method determines whether policy trace messages should be
        /// written, based on the global policy trace flag and the per-interpreter
        /// policy trace flag.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose policy trace flag should be considered, if any.
        /// This value may be null.
        /// </param>
        /// <returns>
        /// True if policy trace messages should be written; otherwise, false.
        /// </returns>
        private static bool ShouldWritePolicyTrace(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            if (GlobalState.PolicyTrace)
                return true;

            if ((interpreter != null) &&
                interpreter.InternalPolicyTrace)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a policy trace message describing the specified
        /// method and name/value parameter pairs, but only when policy tracing
        /// is enabled.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method that originated the policy trace message, if
        /// any.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the policy trace message, if any.
        /// This value may be null.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to include in
        /// the policy trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MaybeWritePolicyTrace(
            string methodName,         /* in */
            Interpreter interpreter,   /* in */
            bool ellipsis,             /* in */
            params object[] parameters /* in */
            )
        {
            bool didWrite;

            MaybeWritePolicyTrace(
                methodName, interpreter, ellipsis, out didWrite,
                parameters);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For (direct) use by Interpreter.CheckPolicies method
        //          only.
        //
        /// <summary>
        /// This method writes a policy trace message describing the specified
        /// method and name/value parameter pairs, but only when policy tracing
        /// is enabled, reporting whether the message was actually written.  It
        /// is intended for direct use by the Interpreter.CheckPolicies method
        /// only.
        /// </summary>
        /// <param name="methodName">
        /// The name of the method that originated the policy trace message, if
        /// any.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the policy trace message, if any.
        /// This value may be null.
        /// </param>
        /// <param name="ellipsis">
        /// Non-zero to truncate long parameter values with an ellipsis.
        /// </param>
        /// <param name="didWrite">
        /// Upon return, receives a value indicating whether the policy trace
        /// message was written; note that this value being non-zero does not
        /// guarantee the tracing subsystem actually emitted the message.
        /// </param>
        /// <param name="parameters">
        /// The array of alternating parameter names and values to include in
        /// the policy trace message.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MaybeWritePolicyTrace(
            string methodName,         /* in */
            Interpreter interpreter,   /* in */
            bool ellipsis,             /* in */
            out bool didWrite,         /* out */
            params object[] parameters /* in */
            )
        {
            didWrite = false;

            if (ShouldWritePolicyTrace(interpreter))
            {
                StringBuilder builder = StringBuilderFactory.Create();

                AppendTraceParameters(
                    TracePriority.None, builder, parameters, ellipsis);

                if (builder.Length > 0)
                {
                    string localMethodName;

                    if (!String.IsNullOrEmpty(methodName))
                        localMethodName = methodName;
                    else
                        localMethodName = "MaybeWritePolicyTrace";

                    DebugTraceAlways(String.Format(
                        "{0}: interpreter = {1}, {2}", localMethodName,
                        FormatOps.InterpreterNoThrow(interpreter), builder),
                        typeof(TraceOps).Name, TracePriority.PolicyTrace);

                    //
                    // BUGBUG: This is actually something of a small "lie".
                    //         The tracing subsystem is free to ignore this
                    //         trace message due to several internal rules
                    //         it enforces, e.g. AppDomain shutdown, wrong
                    //         priority (too low, etc), disabled category,
                    //         throttle limits, etc.
                    //
                    didWrite = true;
                }

                StringBuilderCache.Release(ref builder);
            }
        }
#endif
        #endregion
        #endregion
    }
}
