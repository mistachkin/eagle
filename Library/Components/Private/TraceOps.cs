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
        private static readonly Regex DefaultTraceCategoryRegEx = RegExOps.Create(
            "^[\\.0-9A-Z_]*$", RegexOptions.IgnoreCase);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This regular expression can be used to determines if a string
        //       is considered to be a valid method name.  By default, this
        //       value will not be used.  To be used, it would need to be set
        //       as the value of the "MethodNameRegEx" field (below).
        //
        private static readonly Regex DefaultMethodNameRegEx = RegExOps.Create(
            "^[\\.0-9A-Z_]*$", RegexOptions.IgnoreCase);
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
        private static Regex TraceCategoryRegEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When set to non-null, this regular expression will be used
        //       to figure out if a method name is considered valid.
        //
        // HACK: This is purposely not read-only.
        //
        private static Regex MethodNameRegEx = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the (initial?) portion of the format string used to
        //       indicate that the tracing subsystem was somehow reentered.
        //       There are several ways this could happen, including via use
        //       of static initializers.
        //
        private const string TraceNestedIndicator = "[NESTED] ";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the (effective) format string that is used by the
        //       TraceListener class in the .NET Framework.  There are two
        //       parameter specifiers:
        //
        //       0. The trace category, if any; if this is null, only the
        //          trace message itself will be emitted.  It should be
        //          noted that an empty string is technically valid here.
        //
        //       1. The trace message itself.
        //
        private const string TraceListenerFormat = "{0}: {1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the portion of the format string used to insert the
        //       optional stack trace into the final output.
        //
        private const string TraceStackFormat = "{9}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the portion of the format string used to insert one
        //       or more new lines into the final output.
        //
        private const string TraceNewLineFormat = "{11}";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The various trace formats shared between this class and the
        //       FormatOps class.  The passed format arguments are always the
        //       same; therefore, we just omit the ones we do not need for a
        //       particular format.
        //
        private const string BareTraceFormat = "{10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        private const string MinimumTraceFormat = "{0}{10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        private const string MediumLowTraceFormat = "{0}{1} {10}" +
            TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        private const string MediumTraceFormat = "{0}{7}: {10}" +
            TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        private const string MediumHighTraceFormat =
            "{0}[p:{2}] [s:{3}] [x:{4}] [a:{5}] [i:{6}] [t:{7}] [m:{8}]: " +
            "{10}" + TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

        private const string MaximumTraceFormat =
            "{0}[d:{1}] [p:{2}] [s:{3}] [x:{4}] [a:{5}] [i:{6}] [t:{7}] " +
            "[m:{8}]: {10}" + TraceStackFormat + TraceNewLineFormat;

        ///////////////////////////////////////////////////////////////////////

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
        private const int FormatParamterCount = 12;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This array must be manually kept synchronized with the
        //       values of the TraceFormatType enumeration.
        //
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

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This array MUST be the same size as the flag and
        //          full name arrays (above).
        //
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

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default values for the (overridden?) trace
        //       format string and trace format index.
        //
        private const string DefaultTraceFormatString = null;
        private static readonly int? DefaultTraceFormatIndex = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: What is the fallback trace format when no explicit format
        //       string -OR- format index has been set?
        //
        private const string DefaultFallbackTraceFormat = MediumTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If no trace format string -OR- trace format index has been
        //       explicitly set by the user, should we use a fallback trace
        //       format?
        //
        private const bool DefaultUseFallbackTraceFormat = true; // COMPAT: Eagle beta.

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the "normal" range of trace format indexes.
        //
        private static readonly int MinimumTraceIndex = 2;
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
        private static bool DefaultTracePossible = true;
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
        private static int DefaultMaximumTraceLevels = 2;
        private static int DefaultMaximumWriteLevels = 2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: What is the fallback trace format when no explicit format
        //       string -OR- format index has been set?
        //
        private static string FallbackTraceFormat = DefaultFallbackTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If no trace format string -OR- trace format index has been
        //       explicitly set by the user, should this class fallback to
        //       using the system default trace format?
        //
        // HACK: This is purposely not read-only.
        //
        private static bool UseFallbackTraceFormat = DefaultUseFallbackTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only; however, they should not
        //       need to be changed.  Instead, the associated methods in this
        //       class can be called.
        //
        private static IntDictionary DefaultTraceCategories = null;

        private static TracePriority DefaultTracePriority =
            TracePriority.Default;

        private static TracePriority DefaultTracePriorities =
            TracePriority.DefaultMask;

        private static TracePriority DefaultGlobalPriorities =
            TracePriority.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int DefaultCategoryPenalty = -1;
        private static int DefaultCategoryBonus = 1;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string EnabledName =
            TraceCategoryType.Enabled.ToString().ToLowerInvariant();

        private static readonly string DisabledName =
            TraceCategoryType.Disabled.ToString().ToLowerInvariant();

        private static readonly string PenaltyName =
            TraceCategoryType.Penalty.ToString().ToLowerInvariant();

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
        private static int isTraceInitialized;

        //
        // NOTE: This field helps determine what the IsTracePossible method
        //       will return.  If this field is zero, no "trace" handling of
        //       any kind will be performed, including the normal formatting
        //       and category checks, etc.
        //
        private static bool isTracePossible = DefaultTracePossible;

        //
        // NOTE: This field helps determine what the IsWritePossible method
        //       will return.  If this field is zero, no "write" handling of
        //       any kind will be performed, including the normal formatting
        //       and category checks, etc.
        //
        private static bool isWritePossible = DefaultWritePossible;

        //
        // NOTE: Current number of calls to DebugTrace() that are active on
        //       this thread.  This number should always be zero or one.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static int traceLevels = 0;

        //
        // NOTE: Current number of calls to DebugWriteTo() that are active
        //       on this thread.  This number should always be zero or one.
        //
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
        private static IntDictionary enabledTraceCategories;
        private static IntDictionary disabledTraceCategories;

        //
        // NOTE: This is the dictionary of trace categories that are eligible
        //       for a trace priority "penalty" or "bonus", respectively.
        //
        private static IntDictionary penaltyTraceCategories;
        private static IntDictionary bonusTraceCategories;

        //
        // NOTE: These fields help determine what the IsTraceEnabled method
        //       will return.  They are used to check if the specified trace
        //       priority matches this mask of enabled trace priorities.
        //
        private static TracePriority tracePriorities;
        private static TracePriority globalPriorities;

        //
        // NOTE: This is the default trace priority value used when a method
        //       overload that lacks such a parameter is used.
        //
        private static TracePriority defaultTracePriority;

        //
        // NOTE: This field determines if core library tracing is enabled or
        //       disabled by default.  The value of this field is only used
        //       when initializing this subsystem and then only if both the
        //       NoTrace and Trace environment variables are not set [to
        //       anything].
        //
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
        private static bool? isTraceEnabled = null;

        //
        // NOTE: This is the callback to consult when performing filtering
        //       without an interpreter context or its trace filter callback.
        //
        private static TraceFilterCallback traceFilterCallback;

        //
        // NOTE: When set to non-zero, all trace messages will be redirected
        //       to the associated interpreter host, if applicable.  Caution
        //       should be taken when setting this to non-zero this because
        //       that could easily result in a deadlock, depending on which
        //       locks are held by the current thread.
        //
        private static int isTraceToInterpreterHost = 0;

        //
        // NOTE: This is the number of nesting levels for emitting traces to
        //       the interpreter host, if applicable.  It is used to prevent
        //       any reentrancy into the interpreter host redirection code.
        //
        private static int traceToInterpreterHostLevels = 0;

        //
        // NOTE: This is the current trace format string.  Normally, this is
        //       set to null.  It can be set to any valid format string as
        //       long as out-of-bounds argument (string replacement) indexes
        //       are not used.
        //
        private static string traceFormatString;

        //
        // NOTE: This is the current trace format index.  Normally, this is
        //       set to null.  It can be set to any valid format index.
        //
        private static int? traceFormatIndex;

        //
        // NOTE: When this value is non-zero, the formatted DateTime (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        private static bool traceDateTime;

        //
        // NOTE: When this value is non-zero, the trace priority value will be
        //       included in the trace output; otherwise, it will be replaced
        //       with the string "<null>" or similar.
        //
        private static bool tracePriority;

        //
        // NOTE: When this value is non-zero, the machine name of the server
        //       (if any) will be included in the trace output; otherwise, it
        //       will be replaced with the string "<null>" or similar.
        //
        private static bool traceServerName;

        //
        // NOTE: When this value is non-zero, the active test name (if any)
        //       will be included in the trace output; otherwise, it will
        //       be replaced with the string "<null>" or similar.
        //
        private static bool traceTestName;

        //
        // NOTE: When this value is non-zero, the active application domain (if
        //       any) will be included in the trace output; otherwise, it will
        //       be replaced with the string "<null>" or similar.
        //
        private static bool traceAppDomain;

        //
        // NOTE: When this value is non-zero, the active interpreter (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        private static bool traceInterpreter;

        //
        // NOTE: When this value is non-zero, the active thread (if any) will
        //       be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        private static bool traceThreadId;

        //
        // NOTE: When this value is non-zero, the active method name (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        private static bool traceMethod;

        //
        // NOTE: When this value is non-zero, the complete call stack (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        private static bool traceStack;

        //
        // NOTE: When this value is non-zero, surround all trace messages with
        //       at least one new line before and after to help make them more
        //       readable.
        //
        private static bool traceExtraNewLines;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       emitted due to the subsystem not being (fully?) usable.
        //
        private static long traceImpossible = 0;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       emitted due to having an excluded priority and/or category.
        //
        private static long traceDisabled = 0;

        //
        // NOTE: This is the total number of trace messages that have NOT been
        //       emitted due to being too noisy, duplicates, etc.
        //
        private static long traceTripped = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       filtered out (ever).
        //
        private static long traceFiltered = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       caused an exception to be caught within the trace message
        //       output pipeline.
        //
        private static long traceException = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       emitted (ever).
        //
        private static long traceEmitted = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       logged (ever).
        //
        private static long traceLogged = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       dropped for any reason (ever).
        //
        private static long traceDropped = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       seen due to lock warnings.
        //
        private static long traceLockWarnings = 0;

        //
        // NOTE: This is the total number of trace messages that have been
        //       seen due to lock errors.
        //
        private static long traceLockErrors = 0;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        public static void TryLock(
            ref bool locked /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExitLock(
            ref bool locked /* in, out */
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
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
                //       may be blocked or emitted when they should have
                //       been emitted or blocked, respectively.
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

                if (empty || (traceEmitted != 0))
                {
                    localList.Add("TraceEmitted",
                        traceEmitted.ToString());
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

                list.Add("forceToListeners",
                    DebugOps.GetForceToListeners().ToString());

                list.Add("isEnabled", (isTraceEnabled != null) ?
                    ((bool)isTraceEnabled).ToString() : null);

                list.Add("isFiltered", isFiltered.ToString());

                list.Add("areLimitsEnabled",
                    TraceLimits.IsEnabled().ToString());

                list.Add("priority",
                    GetTracePriority().ToString());

                list.Add("priorities",
                    GetTracePriorities().ToString());

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
                /* NO RESULT */
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
                    /* NO RESULT */
                    DebugOps.SetForceToListeners(enabled);

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
            useIndicators = traceClientData.UseIndicators;
            rawIndicators = traceClientData.RawIndicators;
            seeListeners = traceClientData.SeeListeners;
        }

        ///////////////////////////////////////////////////////////////////////

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
                out useIndicators, out rawIndicators, out seeListeners);

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
#if TEST
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

            result = traceClientData.Results;
            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
#if CONSOLE
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

        private static bool IsTracePossible()
        {
            /* NO-LOCK */
            return isTracePossible && !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWritePossible()
        {
            /* NO-LOCK */
            return isWritePossible && !AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool IsTracePending()
        {
            return Interlocked.CompareExchange(ref traceLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static int FindTraceCategory(
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

        //
        // NOTE: This method assumes the static lock is held.
        //
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

        public static string GetTracePriorityName(
            TracePriority priority, /* in */
            bool shortName          /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
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
        private static bool HasTracePriorities(
            TracePriority flags,    /* in */
            TracePriority hasFlags, /* in */
            bool all                /* in */
            )
        {
            return FlagOps.HasFlags(
                flags, hasFlags & TracePriority.HasPrioritiesMask, all);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static bool IsTraceEnabled(
            TracePriority priority,    /* in */
            params string[] categories /* in */
            )
        {
            bool result;

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
                result = (bool)isTraceEnabled;

                //
                // NOTE: If tracing has been globally disabled, do not bother
                //       checking any categories.
                //
                if (result)
                {
                    /* NO RESULT */
                    MaybeInitialize();

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
                            FlagOps.HasFlags(tracePriorities,
                                TracePriority.CategoryBonus, true) &&
                            (FindTraceCategory(tracePriorities, categories,
                                bonusTraceCategories) != Index.Invalid))
                        {
                            categoryBonus = DefaultCategoryBonus;
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
                            FlagOps.HasFlags(tracePriorities,
                                TracePriority.CategoryPenalty, true) &&
                            (FindTraceCategory(tracePriorities, categories,
                                penaltyTraceCategories) != Index.Invalid))
                        {
                            categoryPenalty = DefaultCategoryPenalty;
                            goto retry;
                        }

                        //
                        // NOTE: If the caller specified a null category -OR-
                        //       there are no trace categories specifically
                        //       enabled (i.e. all trace categories are
                        //       allowed), always allow the message through.
                        //
                        if ((categories != null) && (categories.Length > 0))
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
                                if (FindTraceCategory(tracePriorities, categories,
                                        enabledTraceCategories) == Index.Invalid)
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
                                if (FindTraceCategory(tracePriorities, categories,
                                        disabledTraceCategories) != Index.Invalid)
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                }
            }

        done:

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetTraceToInterpreterHost()
        {
            return Interlocked.CompareExchange(
                ref isTraceToInterpreterHost, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetTracePossible()
        {
            lock (syncRoot)
            {
                isTracePossible = DefaultTracePossible;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetWritePossible() /* NOT USED */
        {
            lock (syncRoot)
            {
                isWritePossible = DefaultWritePossible;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetTraceEnabled()
        {
            lock (syncRoot)
            {
                isTraceEnabled = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static TraceFilterCallback GetTraceFilterCallback()
        {
            lock (syncRoot)
            {
                return traceFilterCallback;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetTraceFilterCallback()
        {
            lock (syncRoot)
            {
                traceFilterCallback = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTraceFilterCallback(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter != null)
                interpreter.InternalTraceFilterCallback = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsTraceFiltered(
            Interpreter interpreter, /* in */
            string message,          /* in */
            string category,         /* in */
            TracePriority priority   /* in */
            )
        {
            try
            {
                //
                // HACK: Attempt to invoke the trace filter callback.  If
                //       it throws an exception, we ignore it -AND- allow
                //       the trace to be emitted.  Exceptions here cannot
                //       be allowed to escape this method because that may
                //       cause this class to be called again to report the
                //       exception.
                //
                TraceFilterCallback callback = GetTraceFilterCallback(
                    interpreter);

                if (callback != null)
                {
                    return callback(
                        interpreter, message, category, priority); /* throw */
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
        public static TracePriority GetTracePriorities()
        {
            lock (syncRoot)
            {
                return tracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetTracePriorities()
        {
            lock (syncRoot)
            {
                tracePriorities = DefaultTracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static TracePriority GetGlobalPriorities()
        {
            lock (syncRoot)
            {
                return globalPriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetGlobalPriorities()
        {
            lock (syncRoot)
            {
                globalPriorities = DefaultGlobalPriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static TracePriority GetTracePriority()
        {
            lock (syncRoot)
            {
                return defaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ResetTracePriority()
        {
            lock (syncRoot)
            {
                defaultTracePriority = DefaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static IEnumerable<string> ListTraceCategories()
        {
            return ListTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void SetTraceCategories(
            IEnumerable<string> categories, /* in */
            int value                       /* in */
            )
        {
            SetTraceCategories(TraceCategoryType.Default, categories, value);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void UnsetTraceCategories(
            IEnumerable<string> categories /* in */
            )
        {
            UnsetTraceCategories(TraceCategoryType.Default, categories);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static void ClearTraceCategories()
        {
            ClearTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static TraceStateType ResetTraceCategories()
        {
            return ResetTraceCategories(TraceCategoryType.Default);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static string GetTraceFormatString()
        {
            lock (syncRoot)
            {
                return traceFormatString;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTraceFormatString()
        {
            lock (syncRoot)
            {
                traceFormatString = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static int? GetTraceFormatIndex()
        {
            lock (syncRoot)
            {
                return traceFormatIndex;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTraceFormatIndex()
        {
            lock (syncRoot)
            {
                traceFormatIndex = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
                string[] args = new string[FormatParamterCount];

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
        private static string GetBareTraceFormat()
        {
            return GetBuiltInTraceFormat(1);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetBareTraceFormat()
        {
            return SetBuiltInTraceFormat(1);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsBareTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetBareTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetMinimumTraceFormat()
        {
            return GetBuiltInTraceFormat(2);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMinimumTraceFormat()
        {
            return SetBuiltInTraceFormat(2);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsMinimumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMinimumTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetMediumLowTraceFormat()
        {
            return GetBuiltInTraceFormat(3);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMediumLowTraceFormat()
        {
            return SetBuiltInTraceFormat(3);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsMediumLowTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumLowTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetMediumTraceFormat()
        {
            return GetBuiltInTraceFormat(4);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMediumTraceFormat()
        {
            return SetBuiltInTraceFormat(4);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsMediumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetMediumHighTraceFormat()
        {
            return GetBuiltInTraceFormat(Index.Invalid - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMediumHighTraceFormat()
        {
            return SetBuiltInTraceFormat(Index.Invalid - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsMediumHighTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMediumHighTraceFormat());
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetMaximumTraceFormat()
        {
            return GetBuiltInTraceFormat(Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMaximumTraceFormat()
        {
            return SetBuiltInTraceFormat(Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsMaximumTraceFormat()
        {
            return SharedStringOps.SystemEquals(
                GetEffectiveTraceFormat(), GetMaximumTraceFormat());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Fallback Format Management
        private static string GetFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                return FallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static bool GetUseFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                return UseFallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetUseFallbackTraceFormat()
        {
            lock (syncRoot)
            {
                UseFallbackTraceFormat = DefaultUseFallbackTraceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static void MaybeAddNewLines(
            ref string traceFormat /* in, out */
            )
        {
            traceFormat = String.Format(
                "{0}{1}", traceFormat, TraceNewLineFormat);
        }

        ///////////////////////////////////////////////////////////////////////

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
                DebugOps.Output(e);
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
                    //       that to emit the trace message; otherwise,
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
                        Interlocked.Increment(ref traceEmitted);
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
                    Interlocked.Increment(ref traceEmitted);
                    return true;
                }
            }

            TraceWasDropped(interpreter, message, category, priority);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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
                            interpreter, message, category, priority))
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

                    bool nested = (levels > 1);
                    string methodName = null;

#if TEST
                    bool flushBufferedTraceListeners =
#else
                    /* IGNORED */
#endif
                    DebugTraceRaw(interpreter, FormatOps.TraceOutput(
                        traceFormat, nested ? TraceNestedIndicator : null,
                        traceDateTime ? (DateTime?)TimeOps.GetNow() : null,
                        tracePriority ? (TracePriority?)priority : null,
#if WEB && !NET_STANDARD_20
                        traceServerName ? PathOps.GetServerName() : null,
#endif
                        traceTestName ? TestOps.GetCurrentName(interpreter) : null,
                        traceAppDomain ? AppDomainOps.GetCurrent() : null,
                        traceInterpreter ? interpreter : null,
                        traceThreadId ? threadId : null, message, traceMethod,
                        traceStack, skipFrames + 1, ref category, ref methodName),
                        category, methodName, priority, skipChecks); /* throw */

#if TEST
                    //
                    // HACK: If necessary, flush any IBufferedTraceListener
                    //       that may be present.  Currently, these are not
                    //       "production ready" and are only included if the
                    //       core library is compiled with the TEST option
                    //       defined.
                    //
                    if (flushBufferedTraceListeners)
                        DebugOps.FlushBufferedTraceListeners(false);
#endif
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
                DebugOps.Output(e);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Exception exception,   /* in */
            string category,       /* in */
            string prefix,         /* in */
            TracePriority priority /* in */
            )
        {
            DebugTraceAlways(exception, category, prefix,
                priority | TracePriority.ExtraSkipFrame);
        }

        ///////////////////////////////////////////////////////////////////////

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

            string message = FormatOps.TraceException(exception);

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
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

            string message = FormatOps.TraceException(exception);

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTraceAlways(
            Exception exception,   /* in */
            string category,       /* in */
            string prefix,         /* in */
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

            string message = FormatOps.TraceException(exception);

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
                    String.Format("{0}{1}", prefix,
                    message), category, priority, skipFrames,
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

            string message = FormatOps.TraceException(exception);

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DebugTraceAlways(
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
        private static bool ShouldEmitPolicyTrace(
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MaybeEmitPolicyTrace(
            string methodName,         /* in */
            Interpreter interpreter,   /* in */
            bool ellipsis,             /* in */
            params object[] parameters /* in */
            )
        {
            bool didEmit = false;

            MaybeEmitPolicyTrace(
                methodName, interpreter, ellipsis, ref didEmit, parameters);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For (direct) use by Interpreter.CheckPolicies method
        //          only.
        //
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MaybeEmitPolicyTrace(
            string methodName,         /* in */
            Interpreter interpreter,   /* in */
            bool ellipsis,             /* in */
            ref bool didEmit,          /* out */
            params object[] parameters /* in */
            )
        {
            didEmit = false;

            if (ShouldEmitPolicyTrace(interpreter))
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
                        localMethodName = "MaybeEmitPolicyTrace";

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
                    didEmit = true;
                }

                StringBuilderCache.Release(ref builder);
            }
        }
#endif
        #endregion
        #endregion
    }
}
