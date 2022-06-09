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
        private static TracePriority DefaultPriorityMask =
            TracePriority.DefaultLimitMask;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static int MaximumPerCategoryCount = 5;
        private static TimeSpan MaximumPerCategoryTime = new TimeSpan(0, 1, 0);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static int MaximumCategorySeconds = Count.Invalid;

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // HACK: This is purposely not read-only.
        //
        private static int MaximumMessageCount = 100;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: If this is greater than zero, the public entry points into
        //       this subsystem are disabled (i.e. they are no-ops).
        //
        private static int disableCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Current number of calls to the IsTripped() -OR- KeepTrack()
        //       methods that are active on this thread.  This number should
        //       always be zero or one.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static int isTrippedLevels = 0;

        [ThreadStatic()] /* ThreadSpecificData */
        private static int keepTrackLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        private static int skippedIsTripped = 0;
        private static int skippedKeepTrack = 0;

        ///////////////////////////////////////////////////////////////////////

        private static MessageDictionary messages;

        ///////////////////////////////////////////////////////////////////////

        private static CategoryDictionary categories;
        private static TrippedDictionary trippedCategories;
        private static DateTime clearedCategories;

        ///////////////////////////////////////////////////////////////////////

        private static TracePriorityDictionary priorities;
        private static TrippedDictionary trippedPriorities;

        ///////////////////////////////////////////////////////////////////////

        private static bool trackRawPriority = false; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        private static bool checkMessage = true; // TODO: Good default?
        private static bool checkCategory = true; // TODO: Good default?
        private static bool checkPriority = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        private static bool trackMessage = true; // TODO: Good default?
        private static bool trackCategory = true; // TODO: Good default?
        private static bool trackPriority = true; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
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

            priorities = TraceOps.CreateTracePriorities(priorityMask, 0);
        }

        ///////////////////////////////////////////////////////////////////////

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
                    Count.Invalid, MaximumMessageCount, Count.Invalid,
                    Count.Invalid, Count.Invalid, Count.Invalid);
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

        private static TracePriority MaskPriority(
            TracePriority priority /* in */
            )
        {
            return priority & TracePriority.AnyPriorityMask;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static string DumpMessages(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return FormatOps.DumpDictionary(
                    messages, hashAlgorithmName, raw);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string DumpTrippedCategories(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return FormatOps.DumpDictionary(
                    trippedCategories, hashAlgorithmName, raw);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string DumpTrippedPriorities(
            string hashAlgorithmName,
            bool raw
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return FormatOps.DumpDictionary(
                    trippedPriorities, hashAlgorithmName, raw);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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
