/*
 * TraceLimits.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using TracePriorityDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.TracePriority, int>;

using CategoryDictionary = Eagle._Containers.Private.DateTimeListDictionary;

#if CACHE_DICTIONARY
using MessageDictionary = Eagle._Containers.Private.CacheDictionary<string, int>;
#else
using MessageDictionary = System.Collections.Generic.Dictionary<string, int>;
#endif

using TrippedDictionary = System.Collections.Generic.Dictionary<string, int>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class implements the rate-limiting (throttling) subsystem used by
    /// the tracing facility.  It keeps track of how often individual trace
    /// messages, trace categories, and trace priorities have been seen and
    /// determines when a given trace message should be suppressed because it
    /// has exceeded its configured limit.  All of its state is process-wide
    /// (static) and access to that state is synchronized.
    /// </summary>
    [ObjectId("81cd7f89-92cd-41cb-a030-e6e6d676124a")]
    internal static class TraceLimits
    {
        #region Private Constants
        //
        // TODO: By default, only the lowest priority is included.  Is this a
        //       good default?
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default set of trace priority flags that are subject to limit
        /// checking when no explicit priority limit configuration is present.
        /// </summary>
        private static TracePriority DefaultPriorityMask =
            TracePriority.DefaultLimitMask;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The maximum number of trace messages permitted for any single trace
        /// category within the rolling time window before that category is
        /// considered tripped (i.e. rate-limited).
        /// </summary>
        private static int MaximumPerCategoryCount = 10;
        /// <summary>
        /// The length of the rolling time window over which per-category trace
        /// message counts are evaluated.
        /// </summary>
        private static TimeSpan MaximumPerCategoryTime = new TimeSpan(0, 1, 0);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The maximum age, in seconds, that the tracked category data is
        /// allowed to reach before it is automatically cleared; a negative
        /// value disables this periodic clearing.
        /// </summary>
        private static int MaximumCategorySeconds = Count.Invalid;

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The maximum number of distinct trace messages to retain in the
        /// message cache before older entries are trimmed.
        /// </summary>
        private static int MaximumMessageCount = 100;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to all of the static state
        /// maintained by this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this is greater than zero, the public entry points into
        //       this subsystem are disabled (i.e. they are no-ops).
        //
        /// <summary>
        /// When greater than zero, the public entry points into this subsystem
        /// are disabled (i.e. they behave as no-ops).
        /// </summary>
        private static int disableCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Current number of calls to the IsTripped() -OR- KeepTrack()
        //       methods that are active on this thread.  This number should
        //       always be zero or one.
        //
        /// <summary>
        /// The current number of active (possibly nested) calls to the
        /// <see cref="IsTripped" /> method on this thread; this should always
        /// be zero or one.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static int isTrippedLevels = 0;

        /// <summary>
        /// The current number of active (possibly nested) calls to the
        /// <see cref="KeepTrack" /> method on this thread; this should always
        /// be zero or one.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static int keepTrackLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of times an <see cref="IsTripped" /> check was skipped
        /// because the subsystem was already busy on the current thread.
        /// </summary>
        private static int skippedIsTripped = 0;
        /// <summary>
        /// The number of times a <see cref="KeepTrack" /> update was skipped
        /// because the subsystem was already busy on the current thread.
        /// </summary>
        private static int skippedKeepTrack = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked trace message to the number of times it has been
        /// seen; used to detect and suppress repeated messages.
        /// </summary>
        private static MessageDictionary messages;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked trace category to the timestamps at which it has
        /// been seen; used to enforce the per-category rate limit.
        /// </summary>
        private static CategoryDictionary categories;
        /// <summary>
        /// Maps each trace category that has tripped its limit to the number
        /// of times it has done so.
        /// </summary>
        private static TrippedDictionary trippedCategories;
        /// <summary>
        /// The time at which the tracked category data was last cleared.
        /// </summary>
        private static DateTime clearedCategories;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked trace priority to the number of times it has been
        /// seen; used to detect and suppress messages of a limited priority.
        /// </summary>
        private static TracePriorityDictionary priorities;
        /// <summary>
        /// Maps each trace priority that has tripped its limit to the number
        /// of times it has done so.
        /// </summary>
        private static TrippedDictionary trippedPriorities;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the original (unmasked) trace priority is also
        /// recorded in the tripped priorities, which is useful for debugging.
        /// </summary>
        private static bool trackRawPriority = false; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the per-message limit is consulted when determining
        /// whether a trace message has tripped.
        /// </summary>
        private static bool checkMessage = true; // TODO: Good default?
        /// <summary>
        /// When non-zero, the per-category limit is consulted when determining
        /// whether a trace message has tripped.
        /// </summary>
        private static bool checkCategory = true; // TODO: Good default?
        /// <summary>
        /// When non-zero, the per-priority limit is consulted when determining
        /// whether a trace message has tripped.
        /// </summary>
        private static bool checkPriority = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the occurrence of each trace message is recorded for
        /// rate-limiting purposes.
        /// </summary>
        private static bool trackMessage = true; // TODO: Good default?
        /// <summary>
        /// When non-zero, the occurrence of each trace category is recorded for
        /// rate-limiting purposes.
        /// </summary>
        private static bool trackCategory = true; // TODO: Good default?
        /// <summary>
        /// When non-zero, the occurrence of each trace priority is recorded for
        /// rate-limiting purposes.
        /// </summary>
        private static bool trackPriority = true; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method ensures that all of the internal tracking data
        /// structures used by this subsystem have been created.
        /// </summary>
        /// <param name="force">
        /// Non-zero to recreate every tracking data structure even if it has
        /// already been created.
        /// </param>
        private static void Initialize(
            bool force /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (messages == null))
                    messages = new MessageDictionary();

                if (force || (categories == null))
                {
                    categories = new CategoryDictionary();
                    clearedCategories = TimeOps.GetUtcNow();
                }

                if (force || (trippedCategories == null))
                    trippedCategories = new TrippedDictionary();

                if (force || (priorities == null))
                {
                    InitializePriorities(
                        CommonOps.Environment.GetVariable(
                            EnvVars.TracePriorityLimits));
                }

                if (force || (trippedPriorities == null))
                    trippedPriorities = new TrippedDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the trace priority tracking dictionary from the
        /// supplied priority limit configuration string, falling back to the
        /// default priority mask when it is null or cannot be parsed.
        /// </summary>
        /// <param name="value">
        /// The textual list of trace priority flags to limit, or null to use
        /// the default priority mask.
        /// </param>
        private static void InitializePriorities(
            string value /* in */
            )
        {
            TracePriority priorityMask = DefaultPriorityMask;

            if (value != null)
            {
                //
                // WARNING: This is somewhat dangerous here.  Reasoning:
                //          The EnumOps class ends up using the Parser
                //          class to parse its input as a list and this
                //          can result in a call into the NativeUtility
                //          class (i.e. when the native utility library
                //          support is enabled at compile-time).  Then,
                //          the NativeUtility class can end up calling
                //          into the tracing subsystem, which then ends
                //          up calling back into this class.  The "fix"
                //          for this is to prevent all the public entry
                //          points of this class from being re-entered
                //          via try/finally semantics and a per-thread
                //          levels counter.
                //
                object enumValue = EnumOps.TryParseFlags(
                    null, typeof(TracePriority), priorityMask.ToString(),
                    value, null, true, true, true);

                if (enumValue is TracePriority)
                    priorityMask = (TracePriority)enumValue;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                priorities = TraceOps.CreateTracePriorities(priorityMask, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the tracked category data (and, optionally, the
        /// tripped category data) if the configured maximum age has been
        /// exceeded since it was last cleared.
        /// </summary>
        /// <param name="tripped">
        /// Non-zero to also clear the tripped category data when clearing the
        /// tracked category data.
        /// </param>
        /// <returns>
        /// The total number of category entries that were cleared.
        /// </returns>
        private static int MaybeClearCategories(
            bool tripped /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int count = 0;

                if (MaximumCategorySeconds >= 0)
                {
                    double seconds = 0.0;

                    if (TimeOps.ElapsedSeconds(
                            ref seconds, clearedCategories) &&
                        (seconds > MaximumCategorySeconds))
                    {
                        if (categories != null)
                        {
                            count += categories.Count;
                            categories.Clear();
                        }

                        if (tripped && (trippedCategories != null))
                        {
                            count += trippedCategories.Count;
                            trippedCategories.Clear();
                        }

                        clearedCategories = TimeOps.GetUtcNow();
                    }
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace message has
        /// already tripped its limit.
        /// </summary>
        /// <param name="message">
        /// The trace message to check.
        /// </param>
        /// <returns>
        /// True if the message has tripped its limit; otherwise, false.
        /// </returns>
        private static bool IsTrippedMessage(
            string message /* in */
            )
        {
            if (message == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (messages == null)
                    return false;

                return messages.ContainsKey(message);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records an occurrence of the specified trace message,
        /// incrementing its tracked count.
        /// </summary>
        /// <param name="message">
        /// The trace message to record.
        /// </param>
        /// <returns>
        /// True if the message was recorded; otherwise, false.
        /// </returns>
        private static bool TrackMessage(
            string message /* in */
            )
        {
            if (message == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(false);

#if CACHE_DICTIONARY
                messages.TrimExcess(
                    Count.Invalid, MaximumMessageCount,
                    Count.Invalid, Count.Invalid,
                    Count.Invalid, Count.Invalid);
#endif

                int value;

                if (messages.TryGetValue(message, out value))
                    value += 1;
                else
                    value = 1;

                messages[message] = value;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace category has
        /// exceeded its per-category limit within the rolling time window,
        /// recording it as tripped when it has.
        /// </summary>
        /// <param name="category">
        /// The trace category to check.
        /// </param>
        /// <returns>
        /// True if the category has tripped its limit; otherwise, false.
        /// </returns>
        private static bool IsTrippedCategory(
            string category /* in */
            )
        {
            if (category == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (categories == null)
                    return false;

                int count = categories.CountFrom(
                    category, MaximumPerCategoryTime);

                if (count <= MaximumPerCategoryCount)
                    return false;

                TrackTrippedCategory(category);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records an occurrence of the specified trace category
        /// at the current time, after first clearing any stale category data.
        /// </summary>
        /// <param name="category">
        /// The trace category to record.
        /// </param>
        /// <returns>
        /// True if the category was recorded; otherwise, false.
        /// </returns>
        private static bool TrackCategory(
            string category /* in */
            )
        {
            if (category == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(false);

                /* IGNORED */
                MaybeClearCategories(false);

                if (categories == null)
                    return false;

                DateTime now = categories.Now;
                DateTime epoch = now.Subtract(MaximumPerCategoryTime);

                categories.Add(category, now, epoch);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the specified trace category has tripped
        /// its limit, incrementing its tripped count.
        /// </summary>
        /// <param name="category">
        /// The trace category that has tripped its limit.
        /// </param>
        /// <returns>
        /// True if the tripped category was recorded; otherwise, false.
        /// </returns>
        private static bool TrackTrippedCategory(
            string category /* in */
            )
        {
            if (category == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(false);

                if (trippedCategories == null)
                    return false;

                int value;

                if (trippedCategories.TryGetValue(category, out value))
                    value += 1;
                else
                    value = 1;

                trippedCategories[category] = value;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reduces the specified trace priority to the masked
        /// priority value used for limit tracking.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to mask.
        /// </param>
        /// <returns>
        /// The masked trace priority value.
        /// </returns>
        private static TracePriority MaskPriority(
            TracePriority priority /* in */
            )
        {
            return TraceOps.MaskTracePriority(priority);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace priority is
        /// subject to limiting, recording it as tripped when it is.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to check.
        /// </param>
        /// <returns>
        /// True if the priority is being limited; otherwise, false.
        /// </returns>
        private static bool IsTrippedPriority(
            TracePriority priority /* in */
            )
        {
            //
            // TODO: This is designed to filter out repeated (low-priority)
            //       trace messages; however, this priority check may need
            //       to be enhanced at some point.
            //
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (priorities == null)
                    return false;

                TracePriority anyPriority = MaskPriority(priority);

                if (!priorities.ContainsKey(anyPriority))
                    return false;

                TrackTrippedPriority(anyPriority);

                if (trackRawPriority)
                {
                    //
                    // NOTE: This is somewhat useful for debugging (only).
                    //
                    TrackTrippedPriority(priority);
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records an occurrence of the specified trace priority,
        /// incrementing the tracked count for its masked value.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to record.
        /// </param>
        /// <returns>
        /// True if the priority was recorded; otherwise, false.
        /// </returns>
        private static bool TrackPriority(
            TracePriority priority /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(false);

                TracePriority anyPriority = MaskPriority(priority);
                int value;

                if (priorities.TryGetValue(anyPriority, out value))
                    value += 1;
                else
                    value = 1;

                priorities[anyPriority] = value;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that the specified trace priority has tripped
        /// its limit, incrementing its tripped count.
        /// </summary>
        /// <param name="priority">
        /// The trace priority that has tripped its limit, or null to do
        /// nothing.
        /// </param>
        /// <returns>
        /// True if the tripped priority was recorded; otherwise, false.
        /// </returns>
        private static bool TrackTrippedPriority(
            TracePriority? priority /* in */
            )
        {
            if (priority == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(false);

                if (trippedPriorities == null)
                    return false;

                string key = priority.ToString();
                int value;

                if (trippedPriorities.TryGetValue(key, out value))
                    value += 1;
                else
                    value = 1;

                trippedPriorities[key] = value;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases the tracked message data.
        /// </summary>
        /// <returns>
        /// The number of message entries that were cleared.
        /// </returns>
        private static int CleanupMessages()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (messages != null)
                {
                    result += messages.Count;

                    messages.Clear();
                    messages = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases both the tracked category data and
        /// the tripped category data.
        /// </summary>
        /// <returns>
        /// The number of category entries that were cleared.
        /// </returns>
        private static int CleanupCategories()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (categories != null)
                {
                    result += categories.Count;

                    categories.Clear();
                    categories = null;
                }

                if (trippedCategories != null)
                {
                    result += trippedCategories.Count;

                    trippedCategories.Clear();
                    trippedCategories = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases both the tracked priority data and
        /// the tripped priority data.
        /// </summary>
        /// <returns>
        /// The number of priority entries that were cleared.
        /// </returns>
        private static int CleanupPriorities()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (priorities != null)
                {
                    result += priorities.Count;

                    priorities.Clear();
                    priorities = null;
                }

                if (trippedPriorities != null)
                {
                    result += trippedPriorities.Count;

                    trippedPriorities.Clear();
                    trippedPriorities = null;
                }

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method appends a human-readable summary of the current state
        /// of this subsystem to the specified list, for introspection
        /// purposes.
        /// </summary>
        /// <param name="list">
        /// The list to which the summary information is appended.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control the level of detail included in the summary.
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

                if (empty || (disableCount != 0))
                {
                    localList.Add("DisableCount",
                        disableCount.ToString());
                }

                if (empty || (isTrippedLevels != 0))
                {
                    localList.Add("IsTrippedLevels",
                        isTrippedLevels.ToString());
                }

                if (empty || (keepTrackLevels != 0))
                {
                    localList.Add("KeepTrackLevels",
                        keepTrackLevels.ToString());
                }

                if (empty || (skippedIsTripped != 0))
                {
                    localList.Add("SkippedIsTripped",
                        skippedIsTripped.ToString());
                }

                if (empty || (skippedKeepTrack != 0))
                {
                    localList.Add("SkippedKeepTrack",
                        skippedKeepTrack.ToString());
                }

                if (empty ||
                    ((messages != null) && (messages.Count > 0)))
                {
                    localList.Add("Messages",
                        (messages != null) ?
                            messages.Count.ToString() :
                            FormatOps.DisplayNull);

                    localList.Add("CountMessages",
                        FormatOps.CountDictionary(messages));
                }

                if (empty ||
                    ((categories != null) && (categories.Count > 0)))
                {
                    localList.Add("Categories",
                        (categories != null) ?
                            categories.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((trippedCategories != null) &&
                    (trippedCategories.Count > 0)))
                {
                    localList.Add("TrippedCategories",
                        (trippedCategories != null) ?
                            trippedCategories.Count.ToString() :
                            FormatOps.DisplayNull);

                    localList.Add("CountTrippedCategories",
                        FormatOps.CountDictionary(trippedCategories));
                }

                if (empty || (clearedCategories != DateTime.MinValue))
                {
                    localList.Add("ClearedCategories",
                        FormatOps.Iso8601FullDateTime(
                            clearedCategories));
                }

                if (empty ||
                    ((priorities != null) && (priorities.Count > 0)))
                {
                    localList.Add("Priorities",
                        (priorities != null) ?
                            priorities.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((trippedPriorities != null) &&
                    (trippedPriorities.Count > 0)))
                {
                    localList.Add("TrippedPriorities",
                        (trippedPriorities != null) ?
                            trippedPriorities.Count.ToString() :
                            FormatOps.DisplayNull);

                    localList.Add("CountTrippedPriorities",
                        FormatOps.CountDictionary(trippedPriorities));
                }

#if CACHE_DICTIONARY
                if (empty || (MaximumMessageCount != 0))
                {
                    localList.Add("MaximumMessageCount",
                        MaximumMessageCount.ToString());
                }
#endif

                if (empty || (MaximumCategorySeconds != 0))
                {
                    localList.Add("MaximumCategorySeconds",
                        MaximumCategorySeconds.ToString());
                }

                if (empty || (MaximumPerCategoryCount != 0))
                {
                    localList.Add("MaximumPerCategoryCount",
                        MaximumPerCategoryCount.ToString());
                }

                if (empty || (MaximumPerCategoryTime.Ticks != 0))
                {
                    localList.Add("MaximumPerCategoryTime",
                        MaximumPerCategoryTime.ToString());
                }

                if (empty || trackRawPriority)
                {
                    localList.Add("TrackRawPriority",
                        trackRawPriority.ToString());
                }

                if (empty || checkMessage)
                    localList.Add("CheckMessage", checkMessage.ToString());

                if (empty || checkCategory)
                    localList.Add("CheckCategory", checkCategory.ToString());

                if (empty || checkPriority)
                    localList.Add("CheckPriority", checkPriority.ToString());

                if (empty || trackMessage)
                    localList.Add("TrackMessage", trackMessage.ToString());

                if (empty || trackCategory)
                    localList.Add("TrackCategory", trackCategory.ToString());

                if (empty || trackPriority)
                    localList.Add("TrackPriority", trackPriority.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Trace Limits");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Debugging Methods
        /// <summary>
        /// This method produces a textual dump of the tracked messages, the
        /// tripped categories, and the tripped priorities, for debugging
        /// purposes.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use when hashing entry keys, or
        /// null to emit the keys verbatim.
        /// </param>
        /// <param name="raw">
        /// Non-zero to emit the raw (unformatted) entry values.
        /// </param>
        /// <returns>
        /// The formatted dump of the internal state.
        /// </returns>
        private static string DumpState(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringBuilder builder = StringBuilderFactory.Create();

                builder.AppendLine("---- messages ----");

                FormatOps.DumpDictionary(
                    messages, builder, hashAlgorithmName, raw);

                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine("------------------");
                builder.AppendLine();
                builder.AppendLine("---- trippedCategories ----");

                FormatOps.DumpDictionary(
                    trippedCategories, builder, hashAlgorithmName, raw);

                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine("---------------------------");
                builder.AppendLine();
                builder.AppendLine("---- trippedPriorities ----");

                FormatOps.DumpDictionary(
                    trippedPriorities, builder, hashAlgorithmName, raw);

                builder.AppendLine();
                builder.AppendLine();
                builder.AppendLine("---------------------------");
                builder.AppendLine();

                return StringBuilderCache.GetStringAndRelease(ref builder);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a textual dump of the tracked messages, for
        /// debugging purposes.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use when hashing entry keys, or
        /// null to emit the keys verbatim.
        /// </param>
        /// <param name="raw">
        /// Non-zero to emit the raw (unformatted) entry values.
        /// </param>
        /// <returns>
        /// The formatted dump of the tracked messages.
        /// </returns>
        private static string DumpMessages(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringBuilder builder = StringBuilderFactory.Create();

                FormatOps.DumpDictionary(
                    messages, builder, hashAlgorithmName, raw);

                return StringBuilderCache.GetStringAndRelease(ref builder);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a textual dump of the tripped categories, for
        /// debugging purposes.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use when hashing entry keys, or
        /// null to emit the keys verbatim.
        /// </param>
        /// <param name="raw">
        /// Non-zero to emit the raw (unformatted) entry values.
        /// </param>
        /// <returns>
        /// The formatted dump of the tripped categories.
        /// </returns>
        private static string DumpTrippedCategories(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringBuilder builder = StringBuilderFactory.Create();

                FormatOps.DumpDictionary(
                    trippedCategories, builder, hashAlgorithmName, raw);

                return StringBuilderCache.GetStringAndRelease(ref builder);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a textual dump of the tripped priorities, for
        /// debugging purposes.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use when hashing entry keys, or
        /// null to emit the keys verbatim.
        /// </param>
        /// <param name="raw">
        /// Non-zero to emit the raw (unformatted) entry values.
        /// </param>
        /// <returns>
        /// The formatted dump of the tripped priorities.
        /// </returns>
        private static string DumpTrippedPriorities(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringBuilder builder = StringBuilderFactory.Create();

                FormatOps.DumpDictionary(
                    trippedPriorities, builder, hashAlgorithmName, raw);

                return StringBuilderCache.GetStringAndRelease(ref builder);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the trace limiting subsystem is
        /// currently enabled, taking into account the disable count, the
        /// relevant environment variable, and the active interpreter.
        /// </summary>
        /// <returns>
        /// True if the subsystem is enabled; otherwise, false.
        /// </returns>
        public static bool IsEnabled()
        {
            if (Interlocked.CompareExchange(
                    ref disableCount, 0, 0) > 0)
            {
                return false;
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.NoTraceLimits))
            {
                return false;
            }

            Interpreter interpreter = Interpreter.GetActive();

            if ((interpreter != null) &&
                interpreter.HasNoTraceLimits())
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally enables or disables the subsystem by
        /// adjusting the disable count, or simply queries it when no
        /// adjustment is requested.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the subsystem (decrement the disable count),
        /// zero to disable it (increment the disable count), or null to leave
        /// it unchanged and only query the current state.
        /// </param>
        /// <returns>
        /// True if the subsystem is disabled after the adjustment (i.e. the
        /// disable count is greater than zero); otherwise, false.
        /// </returns>
        public static bool MaybeAdjustEnabled(
            bool? enable /* in */
            )
        {
            if (enable != null)
            {
                if ((bool)enable)
                {
                    return Interlocked.Decrement(
                        ref disableCount) > 0;
                }
                else
                {
                    return Interlocked.Increment(
                        ref disableCount) > 0;
                }
            }
            else
            {
                return Interlocked.CompareExchange(
                    ref disableCount, 0, 0) > 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forcibly resets the subsystem to its enabled state by
        /// clearing the disable count and, optionally, removing the associated
        /// environment variable.
        /// </summary>
        /// <param name="environment">
        /// Non-zero to also remove the environment variable that disables the
        /// subsystem.
        /// </param>
        /// <returns>
        /// The number of disabling conditions that were reset.
        /// </returns>
        public static int ForceResetEnabled(
            bool environment /* in */
            )
        {
            int result = 0;

            int oldDisableCount = Interlocked.Exchange(
                ref disableCount, 0);

            if (oldDisableCount > 0)
                result++;

            if (environment && CommonOps.Environment.DoesVariableExist(
                    EnvVars.NoTraceLimits))
            {
                /* NO RESULT */
                CommonOps.Environment.UnsetVariable(
                    EnvVars.NoTraceLimits);

                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a trace message with the specified
        /// message text, category, and priority should be suppressed because
        /// it has exceeded one of its configured limits.
        /// </summary>
        /// <param name="message">
        /// The trace message text to check.
        /// </param>
        /// <param name="category">
        /// The trace category to check.
        /// </param>
        /// <param name="priority">
        /// The trace priority to check.
        /// </param>
        /// <returns>
        /// True if the trace message should be suppressed; otherwise, false.
        /// </returns>
        public static bool IsTripped(
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsEnabled())
                return false;

            int levels = Interlocked.Increment(ref isTrippedLevels);

            try
            {
                //
                // HACK: Prevent possible infinite recursion due to
                //       mutually dependent subsystems.  Currently,
                //       this method is not impacted by such issues;
                //       however, that could change in the future.
                //       Currently, the primary problem arises from
                //       the use of EnumOps in this class, which can
                //       result in [indirect] calls to TraceOps and
                //       then back into this class.
                //
                if (levels <= 1)
                {
                    if (checkPriority && !IsTrippedPriority(priority))
                        return false;

                    if (checkMessage && IsTrippedMessage(message))
                        return true;

                    if (checkCategory && IsTrippedCategory(category))
                        return true;
                }
                else
                {
                    //
                    // NOTE: This trace message cannot be checked
                    //       because this subsystem is busy; this
                    //       is not a big deal as this situation
                    //       should be relatively rare (i.e. once
                    //       per AppDomain?).
                    //
                    Interlocked.Increment(ref skippedIsTripped);
                }

                return false;
            }
            finally
            {
                Interlocked.Decrement(ref isTrippedLevels);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records an occurrence of a trace message with the
        /// specified message text, category, and priority, so that subsequent
        /// limit checks can take it into account.
        /// </summary>
        /// <param name="message">
        /// The trace message text to record.
        /// </param>
        /// <param name="category">
        /// The trace category to record.
        /// </param>
        /// <param name="priority">
        /// The trace priority to record.
        /// </param>
        /// <returns>
        /// True if at least one aspect of the trace message was recorded;
        /// otherwise, false.
        /// </returns>
        public static bool KeepTrack(
            string message,        /* in */
            string category,       /* in */
            TracePriority priority /* in */
            )
        {
            if (!IsEnabled())
                return false;

            int levels = Interlocked.Increment(ref keepTrackLevels);

            try
            {
                //
                // HACK: Prevent possible infinite recursion due to
                //       mutually dependent subsystems.  Currently,
                //       the primary problem arises from the use of
                //       EnumOps in this class, which can result in
                //       [indirect] calls to TraceOps and then back
                //       into this class.
                //
                int count = 0;

                if (levels <= 1)
                {
                    if (trackMessage && TrackMessage(message))
                        count++;

                    if (trackCategory && TrackCategory(category))
                        count++;

                    if (trackPriority && TrackPriority(priority))
                        count++;
                }
                else
                {
                    //
                    // NOTE: This trace message cannot be tracked
                    //       because this subsystem is busy; this
                    //       is not a big deal as this situation
                    //       should be relatively rare (i.e. once
                    //       per AppDomain?).
                    //
                    Interlocked.Increment(ref skippedKeepTrack);
                }

                return (count > 0);
            }
            finally
            {
                Interlocked.Decrement(ref keepTrackLevels);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases all of the tracking data maintained
        /// by this subsystem.
        /// </summary>
        /// <returns>
        /// The total number of tracked entries that were cleared.
        /// </returns>
        public static int Cleanup()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                result += CleanupMessages();
                result += CleanupCategories();
                result += CleanupPriorities();

                return result;
            }
        }
        #endregion
    }
}
