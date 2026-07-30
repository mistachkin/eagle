/*
 * CacheDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if FAST_DICTIONARY
using Eagle._Containers.Public;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a generic dictionary that keeps track of when
    /// and how often each of its keys is accessed, allowing the least useful
    /// entries to be trimmed away once the dictionary exceeds a configured
    /// maximum size.  It also tracks how frequently the dictionary is changed
    /// so that callers can decide whether caching should be enabled or
    /// disabled.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the keys in the dictionary.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values in the dictionary.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("8ac7601d-e0f4-406c-9812-f3af76831d89")]
    internal class CacheDictionary<TKey, TValue> :
#if FAST_DICTIONARY
            FastDictionary<TKey, TValue>,
#else
            Dictionary<TKey, TValue>,
#endif
            IDictionary<TKey, TValue>
    {
        #region Private Constants
        /// <summary>
        /// The default minimum number of milliseconds that must elapse
        /// between successive trim operations.
        /// </summary>
        private const double DefaultTrimMilliseconds = 60000.0; /* 1 min */

        /// <summary>
        /// The default length, in milliseconds, of the interval used when
        /// measuring how frequently this dictionary is changed.
        /// </summary>
        private const double DefaultChangeMilliseconds = 30000.0; /* 30 secs */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The time at which excess elements were most recently trimmed from
        /// this dictionary, or null if no trim has been performed.
        /// </summary>
        private DateTime? lastTrim;

        /// <summary>
        /// The number of elements removed during the most recent trim
        /// operation.
        /// </summary>
        private int lastTrimCount;

        /// <summary>
        /// The total number of trim operations that have been performed on
        /// this dictionary.
        /// </summary>
        private int trimCount;

        /// <summary>
        /// The minimum number of milliseconds that must elapse between
        /// successive trim operations.
        /// </summary>
        private double trimMilliseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The time at which the current change-measurement interval began,
        /// or null if no changes have been recorded yet.
        /// </summary>
        private DateTime? changeEpoch;

        /// <summary>
        /// The number of changes that have been recorded since the start of
        /// the current change-measurement interval.
        /// </summary>
        private int changeCount;

        /// <summary>
        /// The length, in milliseconds, of the interval used when measuring
        /// how frequently this dictionary is changed.
        /// </summary>
        private double changeMilliseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The greatest number of elements this dictionary has held at any one
        /// time.
        /// </summary>
        private int maximumCount;

        /// <summary>
        /// The greatest access count recorded for any single key in this
        /// dictionary.
        /// </summary>
        private int maximumAccessCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This dictionary is used to keep track of the last access
        //       time and usage count of each key in the base dictionary.
        //
        /// <summary>
        /// This dictionary maps each key in the base dictionary to the time it
        /// was last accessed and the number of times it has been accessed.
        /// </summary>
        private Dictionary<TKey, DateTimeIntPair> accessed;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This dictionary is used to keep track of the "list" of
        //       keys from the base dictionary accessed at a particular
        //       point in time, sorted in order, from the oldest access
        //       time to the newest access time.
        //
        /// <summary>
        /// This dictionary maps each distinct access time to the set of keys
        /// from the base dictionary that were accessed at that time, sorted
        /// from the oldest access time to the newest.
        /// </summary>
        private SortedDictionary<
            DateTime?, Dictionary<TKey, object>> keysAccessed;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class that is empty and uses the
        /// default equality comparer for the key type.
        /// </summary>
        public CacheDictionary()
            : base()
        {
            Initialize();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is empty and uses the
        /// specified equality comparer for the key type.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use
        /// the default equality comparer for the key type.
        /// </param>
        public CacheDictionary(
            IEqualityComparer<TKey> comparer
            )
            : base(comparer)
        {
            Initialize(0, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is empty, has the
        /// specified initial capacity, and uses the default equality comparer
        /// for the key type.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the dictionary can contain.
        /// </param>
        public CacheDictionary(
            int capacity
            )
            : base(capacity)
        {
            Initialize(capacity, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is empty, has the
        /// specified initial capacity, and uses the specified equality
        /// comparer for the key type.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the dictionary can contain.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use
        /// the default equality comparer for the key type.
        /// </param>
        public CacheDictionary(
            int capacity,
            IEqualityComparer<TKey> comparer
            )
            : base(capacity, comparer)
        {
            Initialize(capacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains elements copied
        /// from the specified dictionary and uses the default equality
        /// comparer for the key type.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new instance.
        /// </param>
        public CacheDictionary(
            IDictionary<TKey, TValue> dictionary
            )
            : base(dictionary)
        {
            Initialize();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains elements copied
        /// from the specified dictionary and uses the specified equality
        /// comparer for the key type.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new instance.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use
        /// the default equality comparer for the key type.
        /// </param>
        public CacheDictionary(
            IDictionary<TKey, TValue> dictionary,
            IEqualityComparer<TKey> comparer
            )
            : base(dictionary, comparer)
        {
            Initialize(0, comparer);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this instance.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized data.
        /// </param>
        protected CacheDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            Initialize();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        #region Constructor Helper Methods
        /// <summary>
        /// This method resets all settings and statistics for this dictionary
        /// and initializes its access-tracking data structures, using a zero
        /// initial capacity and the current key comparer.
        /// </summary>
        private void Initialize()
        {
            Initialize(0, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all settings and statistics for this dictionary
        /// and initializes its access-tracking data structures.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the access-tracking data
        /// structures can contain.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use
        /// the default equality comparer for the key type.
        /// </param>
        private void Initialize(
            int capacity,
            IEqualityComparer<TKey> comparer
            )
        {
            ResetSettings();
            ResetStatistics();

            InitializeAccessed(capacity, comparer);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Helper Methods
        /// <summary>
        /// This method updates the maximum element count seen so far to
        /// reflect the current number of elements in this dictionary, if it is
        /// now larger.
        /// </summary>
        private void UpdateMaximumCount()
        {
            int count = this.Count;

            if (count > maximumCount)
                maximumCount = count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Remove Helper Methods
        /// <summary>
        /// This method selects some keys that are eligible for removal, based
        /// on their access counts, and removes them from this dictionary.
        /// </summary>
        /// <param name="minimumAccessCount">
        /// The minimum access count a key may have to be eligible for removal,
        /// or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count a key may have to be eligible for removal,
        /// or a negative value to impose no maximum.
        /// </param>
        /// <param name="removeCount">
        /// The number of keys that should be selected for removal.
        /// </param>
        /// <param name="foundCount">
        /// Upon return, this is incremented by the number of candidate keys
        /// that were found.
        /// </param>
        /// <param name="removedCount">
        /// Upon return, this is incremented by the number of keys that were
        /// actually removed.
        /// </param>
        private void Remove( /* O(N) */
            int minimumAccessCount,
            int maximumAccessCount,
            int removeCount,
            ref int foundCount,
            ref int removedCount
            )
        {
            IEnumerable<TKey> keys = GetSomeKeys(
                minimumAccessCount, maximumAccessCount,
                removeCount); /* O(N) */

            if (keys != null)
            {
                foreach (TKey key in keys) /* O(M) */
                {
                    foundCount++;

                    if (key == null)
                        continue;

                    if (Remove(key))
                        removedCount++;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Change Count Helper Methods
        /// <summary>
        /// This method records that this dictionary has changed, starting a
        /// new change-measurement interval if one is not already in progress
        /// and incrementing the change count.
        /// </summary>
        private void UpdateChangeCountAndMaybeTouchEpoch()
        {
            if (changeEpoch == null)
                changeEpoch = Now;

            changeCount++;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restarts the change-measurement interval, setting its
        /// start time to the current time and resetting the change count to
        /// zero.
        /// </summary>
        private void TouchChangeEpochAndCount()
        {
            changeEpoch = Now;
            changeCount = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Iteration Helper Methods
        /// <summary>
        /// This method determines whether the access count contained in the
        /// specified pair falls within the specified bounds.
        /// </summary>
        /// <param name="anyPair">
        /// The access-time and access-count pair to examine.
        /// </param>
        /// <param name="minimumAccessCount">
        /// The minimum acceptable access count, or a negative value to impose
        /// no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum acceptable access count, or a negative value to impose
        /// no maximum.
        /// </param>
        /// <returns>
        /// True if the pair is valid and its access count falls within the
        /// specified bounds; otherwise, false.
        /// </returns>
        private static bool HasGoodAccessCounts(
            DateTimeIntPair anyPair,
            int minimumAccessCount,
            int maximumAccessCount
            )
        {
            if (anyPair != null)
            {
                int accessCount = anyPair.Y;

                if ((maximumAccessCount >= 0) &&
                    (accessCount > maximumAccessCount))
                {
                    return false;
                }

                if ((minimumAccessCount >= 0) &&
                    (accessCount < minimumAccessCount))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified collection of keys
        /// already contains at least the requested number of keys.
        /// </summary>
        /// <param name="keys">
        /// The collection of keys to examine.
        /// </param>
        /// <param name="limit">
        /// The number of keys that are needed, or <see cref="Limits.Unlimited" />
        /// if there is no limit.
        /// </param>
        /// <returns>
        /// True if the collection is a list, a limit is in effect, and the
        /// list already contains at least <paramref name="limit" /> keys;
        /// otherwise, false.
        /// </returns>
        private static bool HaveEnoughKeys(
            IEnumerable<TKey> keys,
            int limit
            )
        {
            if (keys == null)
                return false;

            IList<TKey> list = keys as IList<TKey>;

            if (list == null)
                return false;

            if ((limit != Limits.Unlimited) &&
                (list.Count >= limit))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reuses the specified collection of keys as a list when
        /// possible, copying it into a new list only when necessary.
        /// </summary>
        /// <param name="oldKeys">
        /// The existing collection of keys to reuse, or null.
        /// </param>
        /// <returns>
        /// The supplied collection cast to a list when it already is one, a
        /// new list containing its keys when it is some other collection, or
        /// null when no collection was supplied.
        /// </returns>
        private static List<TKey> MaybeUseOldKeys(
            IEnumerable<TKey> oldKeys
            )
        {
            if (oldKeys == null)
                return null;

            List<TKey> keys = oldKeys as List<TKey>;

            if (keys != null)
                return keys;

            return new List<TKey>(oldKeys);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gathers up to the requested number of keys that are the
        /// best candidates for removal from this dictionary, trying the worst
        /// keys first, then the bad keys, then the oldest keys, and finally
        /// the first keys, until enough keys have been found.
        /// </summary>
        /// <param name="minimumAccessCount">
        /// The minimum access count a key may have to be considered, or a
        /// negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count a key may have to be considered, or a
        /// negative value to impose no maximum.
        /// </param>
        /// <param name="limit">
        /// The number of keys to gather, or <see cref="Limits.Unlimited" /> if
        /// there is no limit.
        /// </param>
        /// <returns>
        /// The collection of candidate keys gathered, or null if none were
        /// found.
        /// </returns>
        private IEnumerable<TKey> GetSomeKeys( /* O(N) */
            int minimumAccessCount,
            int maximumAccessCount,
            int limit
            )
        {
            IEnumerable<TKey> keys = null;

            if ((minimumAccessCount >= 0) &&
                (accessed != null) && (keysAccessed != null))
            {
                keys = GetWorstKeys(
                    keys, minimumAccessCount, maximumAccessCount,
                    limit);

                if (!HaveEnoughKeys(keys, limit))
                {
                    keys = GetBadKeys(
                        keys, minimumAccessCount, maximumAccessCount,
                        limit);
                }
            }

            if ((keysAccessed != null) && !HaveEnoughKeys(keys, limit))
                keys = GetOldestKeys(keys, limit);

            if (!HaveEnoughKeys(keys, limit))
                keys = GetFirstKeys(keys, limit);

            return keys;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is only used when there is no "last accessed"
        //       information available.  It provides some keys to remove
        //       from the cache without taking into account how "popular"
        //       they might be.
        //
        /// <summary>
        /// This method gathers up to the requested number of keys simply by
        /// enumerating this dictionary in order, without regard to how
        /// frequently or recently the keys have been accessed.
        /// </summary>
        /// <param name="oldKeys">
        /// An existing collection of keys to which the gathered keys are
        /// added, or null to start a new collection.
        /// </param>
        /// <param name="limit">
        /// The number of keys to gather, or <see cref="Limits.Unlimited" /> if
        /// there is no limit.
        /// </param>
        /// <returns>
        /// The collection of keys gathered, or null if none were found.
        /// </returns>
        private IEnumerable<TKey> GetFirstKeys( /* O(N) */
            IEnumerable<TKey> oldKeys,
            int limit
            )
        {
            List<TKey> keys = MaybeUseOldKeys(oldKeys);

            //
            // NOTE: Gather X of the "first" keys.
            //
            foreach (KeyValuePair<TKey, TValue> pair in this) /* O(N) */
            {
                if (keys == null)
                    keys = new List<TKey>();

                keys.Add(pair.Key);

                //
                // NOTE: Are we now at -OR- over the limit?  If so,
                //       stop now.
                //
                if ((limit != Limits.Unlimited) &&
                    (keys.Count >= limit))
                {
                    break;
                }
            }

            return keys;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gathers up to the requested number of the keys that
        /// were accessed least recently, in order from the oldest access time
        /// to the newest.
        /// </summary>
        /// <param name="oldKeys">
        /// An existing collection of keys to which the gathered keys are
        /// added, or null to start a new collection.
        /// </param>
        /// <param name="limit">
        /// The number of keys to gather, or <see cref="Limits.Unlimited" /> if
        /// there is no limit.
        /// </param>
        /// <returns>
        /// The collection of keys gathered, or null if none were found.
        /// </returns>
        private IEnumerable<TKey> GetOldestKeys( /* O(N) */
            IEnumerable<TKey> oldKeys,
            int limit
            )
        {
            List<TKey> keys = MaybeUseOldKeys(oldKeys);

            if (keysAccessed != null)
            {
                //
                // NOTE: Gather X of the oldest keys.
                //
                foreach (KeyValuePair<DateTime?, Dictionary<TKey, object>>
                        pair in keysAccessed) /* O(N) */
                {
                    //
                    // NOTE: Grab the list of keys associated with this
                    //       point-in-time.  If invalid, just skip it.
                    //
                    Dictionary<TKey, object> localKeys = pair.Value;

                    if (localKeys == null)
                        continue;

                    //
                    // NOTE: This may push us over the limit; however,
                    //       we should not really care about this.
                    //
                    if (keys == null)
                        keys = new List<TKey>();

                    keys.AddRange(localKeys.Keys);

                    //
                    // NOTE: Are we now at -OR- over the limit?  If so,
                    //       stop now.
                    //
                    if ((limit != Limits.Unlimited) &&
                        (keys.Count >= limit))
                    {
                        break;
                    }
                }
            }

            return keys;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gathers up to the requested number of keys whose access
        /// counts fall outside the specified bounds, examining the access
        /// information for every tracked key.
        /// </summary>
        /// <param name="oldKeys">
        /// An existing collection of keys to which the gathered keys are
        /// added, or null to start a new collection.
        /// </param>
        /// <param name="minimumAccessCount">
        /// The minimum access count a key may have to be considered good, or a
        /// negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count a key may have to be considered good, or a
        /// negative value to impose no maximum.
        /// </param>
        /// <param name="limit">
        /// The number of keys to gather, or <see cref="Limits.Unlimited" /> if
        /// there is no limit.
        /// </param>
        /// <returns>
        /// The collection of keys gathered, or null if none were found.
        /// </returns>
        private IEnumerable<TKey> GetBadKeys( /* O(N) */
            IEnumerable<TKey> oldKeys,
            int minimumAccessCount,
            int maximumAccessCount,
            int limit
            )
        {
            List<TKey> keys = MaybeUseOldKeys(oldKeys);

            if (accessed != null)
            {
                foreach (KeyValuePair<TKey, DateTimeIntPair> pair in accessed)
                {
                    DateTimeIntPair anyPair = pair.Value;

                    if (!HasGoodAccessCounts(anyPair,
                            minimumAccessCount, maximumAccessCount))
                    {
                        if (keys == null)
                            keys = new List<TKey>();

                        keys.Add(pair.Key);

                        //
                        // NOTE: Are we now at -OR- over the limit?
                        //       If so, stop now.
                        //
                        if ((limit != Limits.Unlimited) &&
                            (keys.Count >= limit))
                        {
                            break;
                        }
                    }
                }
            }

            return keys;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gathers up to the requested number of the worst keys,
        /// those whose access counts fall outside the specified bounds,
        /// examining the keys in order from the oldest access time to the
        /// newest.
        /// </summary>
        /// <param name="oldKeys">
        /// An existing collection of keys to which the gathered keys are
        /// added, or null to start a new collection.
        /// </param>
        /// <param name="minimumAccessCount">
        /// The minimum access count a key may have to be considered good, or a
        /// negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count a key may have to be considered good, or a
        /// negative value to impose no maximum.
        /// </param>
        /// <param name="limit">
        /// The number of keys to gather, or <see cref="Limits.Unlimited" /> if
        /// there is no limit.
        /// </param>
        /// <returns>
        /// The collection of keys gathered, or null if none were found.
        /// </returns>
        private IEnumerable<TKey> GetWorstKeys( /* O(N) */
            IEnumerable<TKey> oldKeys,
            int minimumAccessCount,
            int maximumAccessCount,
            int limit
            )
        {
            List<TKey> keys = MaybeUseOldKeys(oldKeys);

            if ((accessed != null) && (keysAccessed != null))
            {
                //
                // NOTE: Gather X of the "worst" keys, those which are
                //       hopefully either too old -OR- too infrequently
                //       accessed to be useful.
                //
                foreach (KeyValuePair<DateTime?, Dictionary<TKey, object>>
                        pair in keysAccessed) /* O(N) */
                {
                    //
                    // NOTE: Grab the list of keys associated with this
                    //       point-in-time.  If invalid, just skip it.
                    //
                    Dictionary<TKey, object> localKeys = pair.Value;

                    if (localKeys == null)
                        continue;

                    //
                    // NOTE: Check all the keys for this point in time.
                    //       Only those that fall below the specified
                    //       usage count will be added.
                    //
                    bool done = false;

                    foreach (TKey key in localKeys.Keys)
                    {
                        if (key == null)
                            continue;

                        DateTimeIntPair anyPair;

                        if (!accessed.TryGetValue(key, out anyPair))
                            continue;

                        if (!HasGoodAccessCounts(anyPair,
                                minimumAccessCount, maximumAccessCount))
                        {
                            //
                            // NOTE: This may push us over the limit;
                            //       however, we should not really care
                            //       about this.
                            //
                            if (keys == null)
                                keys = new List<TKey>();

                            keys.Add(key);

                            //
                            // NOTE: Are we now at -OR- over the limit?
                            //       If so, stop now.
                            //
                            if ((limit != Limits.Unlimited) &&
                                (keys.Count >= limit))
                            {
                                done = true;
                                break;
                            }
                        }
                    }

                    //
                    // NOTE: Are we done yet?
                    //
                    if (done)
                        break;
                }
            }

            return keys;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Last Accessed Helper Methods
        /// <summary>
        /// Gets the current coordinated universal time (UTC).
        /// </summary>
        private static DateTime Now
        {
            get { return TimeOps.GetUtcNow(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the access-tracking data structures using a
        /// zero initial capacity and the current key comparer, creating them
        /// only if they do not already exist.
        /// </summary>
        private void InitializeAccessed()
        {
            InitializeAccessed(0, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the access-tracking data structures,
        /// creating them only if they do not already exist.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the access-tracking dictionary
        /// can contain.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use when comparing keys, or null to use
        /// the default equality comparer for the key type.
        /// </param>
        private void InitializeAccessed(
            int capacity,
            IEqualityComparer<TKey> comparer
            )
        {
            if (accessed == null)
            {
                accessed = new Dictionary<TKey, DateTimeIntPair>(
                    capacity, comparer);
            }

            if (keysAccessed == null)
            {
                keysAccessed = new SortedDictionary<
                    DateTime?, Dictionary<TKey, object>>();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the access-tracking data structures and,
        /// optionally, discards them entirely.
        /// </summary>
        /// <param name="reset">
        /// Non-zero to discard the access-tracking data structures after
        /// clearing them; zero to merely empty them.
        /// </param>
        private void ClearAccessed(
            bool reset
            )
        {
            if (accessed != null)
            {
                accessed.Clear();

                if (reset)
                    accessed = null;
            }

            if (keysAccessed != null)
            {
                keysAccessed.Clear();

                if (reset)
                    keysAccessed = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the time at which the specified key was last
        /// accessed.
        /// </summary>
        /// <param name="key">
        /// The key whose last access time is to be returned.
        /// </param>
        /// <returns>
        /// The time at which the key was last accessed, or null if the key is
        /// not being tracked or has no recorded access time.
        /// </returns>
        private DateTime? GetAccessed(
            TKey key
            )
        {
            DateTimeIntPair anyPair = GetAccessedAndCount(key);

            if (anyPair == null)
                return null;

            return anyPair.X;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the last access time and access count recorded
        /// for the specified key.
        /// </summary>
        /// <param name="key">
        /// The key whose access information is to be returned.
        /// </param>
        /// <returns>
        /// The access-time and access-count pair for the key, or null if the
        /// key is not being tracked.
        /// </returns>
        private DateTimeIntPair GetAccessedAndCount(
            TKey key
            )
        {
            DateTimeIntPair anyPair;

            if ((key != null) && (accessed != null) &&
                accessed.TryGetValue(key, out anyPair) &&
                (anyPair != null))
            {
                return anyPair;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records an access to the specified key, updating its
        /// last access time and access count and keeping the time-ordered key
        /// index in sync.
        /// </summary>
        /// <param name="key">
        /// The key that was accessed.
        /// </param>
        /// <param name="dateTime">
        /// The time at which the key was accessed, or null if the key has no
        /// expiration.
        /// </param>
        /// <param name="count">
        /// The amount by which to adjust the access count for the key, or null
        /// to leave it unchanged.
        /// </param>
        /// <param name="add">
        /// Non-zero to add tracking information for the key when it is not yet
        /// being tracked; zero to update only existing tracking information.
        /// </param>
        private void UpdateAccessedAndCount(
            TKey key,
            DateTime? dateTime,
            int? count,
            bool add
            )
        {
            if (key == null)
                return;

            //
            // NOTE: Previous time this key was accessed, if any.
            //
            DateTime? oldDateTime = null;

            if (accessed != null)
            {
                //
                // NOTE: The date may be null here (i.e. it never expires).
                //
                DateTimeIntPair anyPair;

                if (!accessed.TryGetValue(key, out anyPair))
                {
                    if (add)
                    {
                        anyPair = DateTimeIntPair.Create(dateTime);
                        accessed.Add(key, anyPair);
                    }
                }
                else if (anyPair != null)
                {
                    //
                    // NOTE: Only grab the old date if the key was added at
                    //       a prior point.
                    //
                    oldDateTime = anyPair.X;
                }

                //
                // NOTE: Update the access count for this key, updating the
                //       maximum access count seen so far if necessary.
                //
                if (anyPair != null)
                {
                    int accessCount = anyPair.Touch(dateTime, count);

                    if (accessCount > maximumAccessCount)
                        maximumAccessCount = accessCount;
                }
            }

            //
            // BUGFIX: Even when not adding, the DateTime entry may need
            //         to be removed from this dictionary if it was just
            //         updated in the accessed dictionary; otherwise,
            //         things start to get badly out of sync.
            //
            if ((dateTime != null) && (keysAccessed != null))
            {
                if (oldDateTime != null)
                {
                    Dictionary<TKey, object> oldKeys;

                    if (keysAccessed.TryGetValue(
                            oldDateTime, out oldKeys) &&
                        (oldKeys != null) && oldKeys.Remove(key) &&
                        (oldKeys.Count == 0))
                    {
                        keysAccessed.Remove(oldDateTime);
                    }
                }

                if (add)
                {
                    Dictionary<TKey, object> newKeys;

                    if (keysAccessed.TryGetValue(dateTime, out newKeys))
                    {
                        if (newKeys != null)
                        {
                            newKeys.Add(key, null);
                        }
                        else
                        {
                            newKeys = new Dictionary<TKey, object>(
                                this.Comparer);

                            newKeys.Add(key, null);

                            keysAccessed[dateTime] = newKeys;
                        }
                    }
                    else
                    {
                        newKeys = new Dictionary<TKey, object>(
                            this.Comparer);

                        newKeys.Add(key, null);

                        keysAccessed.Add(dateTime, newKeys);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all access-tracking information for the
        /// specified key, including its entry in the time-ordered key index.
        /// </summary>
        /// <param name="key">
        /// The key whose access-tracking information is to be removed.
        /// </param>
        /// <param name="dateTime">
        /// The last access time recorded for the key, used to locate its entry
        /// in the time-ordered key index, or null if it has none.
        /// </param>
        private void RemoveAccessed(
            TKey key,
            DateTime? dateTime
            )
        {
            if ((key != null) && (accessed != null))
                accessed.Remove(key);

            if ((dateTime != null) && (keysAccessed != null))
            {
                Dictionary<TKey, object> keys;

                if (keysAccessed.TryGetValue(dateTime, out keys) &&
                    (keys != null) && keys.Remove(key) &&
                    (keys.Count == 0))
                {
                    keysAccessed.Remove(dateTime);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data Helper Methods
        /// <summary>
        /// This method resets the configurable settings of this dictionary to
        /// their default values.
        /// </summary>
        private void ResetSettings()
        {
            trimMilliseconds = DefaultTrimMilliseconds;
            changeMilliseconds = DefaultChangeMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all trim and change statistics gathered by this
        /// dictionary to their initial values.
        /// </summary>
        private void ResetStatistics()
        {
            lastTrim = null; lastTrimCount = 0; trimCount = 0;
            changeEpoch = null; changeCount = 0;

            maximumCount = 0;
            maximumAccessCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Last Trim Helper Methods
        /// <summary>
        /// This method returns the number of milliseconds that have elapsed
        /// since excess elements were most recently trimmed from this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The number of milliseconds since the most recent trim, or
        /// <see cref="Milliseconds.Never" /> if trimming is disabled or no
        /// trim has been performed.
        /// </returns>
        private double GetLastTrimMilliseconds()
        {
            if ((trimMilliseconds < 0.0) || (lastTrim == null))
                return Milliseconds.Never;

            return Now.Subtract(
                (DateTime)lastTrim).TotalMilliseconds;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Change Epoch Helper Methods
        /// <summary>
        /// This method returns the number of milliseconds that have elapsed
        /// since the start of the current change-measurement interval.
        /// </summary>
        /// <returns>
        /// The number of milliseconds since the start of the current
        /// change-measurement interval, or <see cref="Milliseconds.Never" />
        /// if change measurement is disabled or no interval has begun.
        /// </returns>
        private double GetChangeEpochMilliseconds()
        {
            if ((changeMilliseconds < 0.0) || (changeEpoch == null))
                return Milliseconds.Never;

            return Now.Subtract(
                (DateTime)changeEpoch).TotalMilliseconds;
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<KeyValuePair<TKey, TValue>> Overrides
        /// <summary>
        /// This method is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="item">
        /// The key/value pair that would have been added to this dictionary.
        /// </param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws
        /// <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="item">
        /// The key/value pair that would have been removed from this
        /// dictionary.
        /// </param>
        /// <returns>
        /// This method does not return; it always throws an exception.
        /// </returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<TKey, TValue> Overrides
        /// <summary>
        /// Gets or sets the value associated with the specified key, recording
        /// an access to that key.
        /// </summary>
        /// <param name="key">
        /// The key whose value is to be retrieved or assigned.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                TValue value = base[key]; /* throw */

                UpdateAccessedAndCount(key, Now, 1, false);

                return value;
            }
            set
            {
                base[key] = value; /* throw */

                UpdateAccessedAndCount(key, Now, 1, true);
                UpdateMaximumCount();
                UpdateChangeCountAndMaybeTouchEpoch();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified key and value to this dictionary,
        /// recording an access to the key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
        void IDictionary<TKey, TValue>.Add(
            TKey key,
            TValue value
            )
        {
            base.Add(key, value); /* throw */

            UpdateAccessedAndCount(key, Now, 1, true);
            UpdateMaximumCount();
            UpdateChangeCountAndMaybeTouchEpoch();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this dictionary contains the
        /// specified key, recording an access to the key when it is found.
        /// </summary>
        /// <param name="key">
        /// The key to locate in this dictionary.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an element with the specified key;
        /// otherwise, false.
        /// </returns>
        bool IDictionary<TKey, TValue>.ContainsKey(
            TKey key
            )
        {
            bool result = base.ContainsKey(key); /* throw */

            if (result)
                UpdateAccessedAndCount(key, Now, 1, false);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the element with the specified key from this
        /// dictionary, also removing any access-tracking information for the
        /// key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to remove.
        /// </param>
        /// <returns>
        /// True if the element was found and removed; otherwise, false.
        /// </returns>
        bool IDictionary<TKey, TValue>.Remove(
            TKey key
            )
        {
            RemoveAccessed(key, GetAccessed(key));
            UpdateChangeCountAndMaybeTouchEpoch();

            return base.Remove(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value associated with the specified key,
        /// recording an access to the key when it is found.
        /// </summary>
        /// <param name="key">
        /// The key whose value is to be retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the value associated with the specified
        /// key; upon failure, this receives the default value for the value
        /// type.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an element with the specified key;
        /// otherwise, false.
        /// </returns>
        bool IDictionary<TKey, TValue>.TryGetValue(
            TKey key,
            out TValue value
            )
        {
            bool result = base.TryGetValue(key, out value); /* throw */

            if (result)
                UpdateAccessedAndCount(key, Now, 1, false);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<TKey, TValue> Overrides
        /// <summary>
        /// Gets or sets the value associated with the specified key, recording
        /// an access to that key.
        /// </summary>
        /// <param name="key">
        /// The key whose value is to be retrieved or assigned.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
        public virtual new TValue this[TKey key]
        {
            get
            {
                TValue value = base[key]; /* throw */

                UpdateAccessedAndCount(key, Now, 1, false);

                return value;
            }
            set
            {
                base[key] = value; /* throw */

                UpdateAccessedAndCount(key, Now, 1, true);
                UpdateMaximumCount();
                UpdateChangeCountAndMaybeTouchEpoch();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified key and value to this dictionary,
        /// recording an access to the key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
        public virtual new void Add(
            TKey key,
            TValue value
            )
        {
            base.Add(key, value); /* throw */

            UpdateAccessedAndCount(key, Now, 1, true);
            UpdateMaximumCount();
            UpdateChangeCountAndMaybeTouchEpoch();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this dictionary contains the
        /// specified key, recording an access to the key when it is found.
        /// </summary>
        /// <param name="key">
        /// The key to locate in this dictionary.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an element with the specified key;
        /// otherwise, false.
        /// </returns>
        public virtual new bool ContainsKey(
            TKey key
            )
        {
            bool result = base.ContainsKey(key); /* throw */

            if (result)
                UpdateAccessedAndCount(key, Now, 1, false);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the element with the specified key from this
        /// dictionary, also removing any access-tracking information for the
        /// key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to remove.
        /// </param>
        /// <returns>
        /// True if the element was found and removed; otherwise, false.
        /// </returns>
        public virtual new bool Remove(
            TKey key
            )
        {
            RemoveAccessed(key, GetAccessed(key));
            UpdateChangeCountAndMaybeTouchEpoch();

            return base.Remove(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the value associated with the specified key,
        /// recording an access to the key when it is found.
        /// </summary>
        /// <param name="key">
        /// The key whose value is to be retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the value associated with the specified
        /// key; upon failure, this receives the default value for the value
        /// type.
        /// </param>
        /// <returns>
        /// True if this dictionary contains an element with the specified key;
        /// otherwise, false.
        /// </returns>
        public virtual new bool TryGetValue(
            TKey key,
            out TValue value
            )
        {
            bool result = base.TryGetValue(key, out value); /* throw */

            if (result)
                UpdateAccessedAndCount(key, Now, 1, false);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        /// <summary>
        /// This method removes all elements from this dictionary, discarding
        /// all access-tracking information and resetting all statistics.
        /// </summary>
        public virtual new void Clear()
        {
            ClearAccessed(true);
            ResetStatistics();

            base.Clear();

            InitializeAccessed();
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_STANDARD_20 && NET_STANDARD_21
        /// <summary>
        /// This method attempts to add the specified key and value to this
        /// dictionary, recording an access to the key when it is added.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
        /// <returns>
        /// True if the key and value were added; false if an element with the
        /// same key already exists.
        /// </returns>
        public virtual new bool TryAdd(
            TKey key,
            TValue value
            )
        {
            bool result = base.TryAdd(key, value);

            if (result)
            {
                UpdateAccessedAndCount(key, Now, 1, true);
                UpdateMaximumCount();
                UpdateChangeCountAndMaybeTouchEpoch();
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the element with the specified key from this
        /// dictionary and returns its value, also removing any access-tracking
        /// information for the key.
        /// </summary>
        /// <param name="key">
        /// The key of the element to remove.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the value that was associated with the
        /// specified key; upon failure, this receives the default value for
        /// the value type.
        /// </param>
        /// <returns>
        /// True if the element was found and removed; otherwise, false.
        /// </returns>
        public virtual new bool Remove(
            TKey key,
            out TValue value
            )
        {
            RemoveAccessed(key, GetAccessed(key));
            UpdateChangeCountAndMaybeTouchEpoch();

            return base.Remove(key, out value);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        /// <summary>
        /// This method populates the specified serialization information with
        /// the data needed to serialize this dictionary, including its
        /// statistics and access-tracking data.
        /// </summary>
        /// <param name="info">
        /// The object that receives the serialized data for this instance.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source and destination of
        /// the serialized data.
        /// </param>
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            info.AddValue("lastTrim", lastTrim);
            info.AddValue("lastTrimCount", lastTrimCount);
            info.AddValue("trimCount", trimCount);
            info.AddValue("trimMilliseconds", trimMilliseconds);
            info.AddValue("changeEpoch", changeEpoch);
            info.AddValue("changeCount", changeCount);
            info.AddValue("changeMilliseconds", changeMilliseconds);
            info.AddValue("maximumCount", maximumCount);
            info.AddValue("maximumAccessCount", maximumAccessCount);
            info.AddValue("accessed", accessed);
            info.AddValue("keysAccessed", keysAccessed);

            base.GetObjectData(info, context);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the time at which excess elements were most recently trimmed
        /// from this dictionary, or null if no trim has been performed.
        /// </summary>
        public virtual DateTime? LastTrim
        {
            get { return lastTrim; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of elements removed during the most recent trim
        /// operation.
        /// </summary>
        public virtual int LastTrimCount
        {
            get { return lastTrimCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the total number of trim operations that have been performed
        /// on this dictionary.
        /// </summary>
        public virtual int TrimCount
        {
            get { return trimCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the minimum number of milliseconds that must elapse
        /// between successive trim operations.
        /// </summary>
        public virtual double TrimMilliseconds
        {
            get { return trimMilliseconds; }
            set { trimMilliseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the time at which the current change-measurement interval
        /// began, or null if no changes have been recorded yet.
        /// </summary>
        public virtual DateTime? ChangeEpoch
        {
            get { return changeEpoch; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the number of changes that have been recorded since the start
        /// of the current change-measurement interval.
        /// </summary>
        public virtual int ChangeCount
        {
            get { return changeCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the length, in milliseconds, of the interval used when
        /// measuring how frequently this dictionary is changed.
        /// </summary>
        public virtual double ChangeMilliseconds
        {
            get { return changeMilliseconds; }
            set { changeMilliseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the greatest number of elements this dictionary has held at
        /// any one time.
        /// </summary>
        public virtual int MaximumCount
        {
            get { return maximumCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the greatest access count recorded for any single key in this
        /// dictionary.
        /// </summary>
        public virtual int MaximumAccessCount
        {
            get { return maximumAccessCount; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether access tracking is currently enabled
        /// for this dictionary.
        /// </summary>
        /// <returns>
        /// True if the access-tracking data structures exist; otherwise,
        /// false.
        /// </returns>
        public virtual bool IsAccessedEnabled()
        {
            return (accessed != null) && (keysAccessed != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables access tracking for this
        /// dictionary, creating the access-tracking data structures when
        /// enabling and discarding them when disabling.
        /// </summary>
        /// <param name="enabled">
        /// Non-zero to enable access tracking; zero to disable it.
        /// </param>
        public virtual void SetAccessedEnabled(
            bool enabled
            )
        {
            if (enabled)
                InitializeAccessed();
            else
                ClearAccessed(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restarts the change-measurement interval, setting its
        /// start time to the current time and resetting the change count to
        /// zero.
        /// </summary>
        public virtual void RestartChanges()
        {
            TouchChangeEpochAndCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks, once the current change-measurement interval
        /// has elapsed, whether the number of recorded changes indicates that
        /// caching should be enabled or disabled, and then restarts the
        /// interval.
        /// </summary>
        /// <param name="minimumChangeCount">
        /// The minimum number of changes per interval that should cause the
        /// cache to be disabled, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumChangeCount">
        /// The maximum number of changes per interval that should cause the
        /// cache to be disabled, or a negative value to impose no maximum.
        /// </param>
        /// <param name="maybeEnable">
        /// Upon return, this is set to true if the caller should enable the
        /// cache, false if the caller should disable it, or left unchanged if
        /// no action is recommended.
        /// </param>
        public virtual void CheckForNoOrExcessChanges(
            int minimumChangeCount, /* in */
            int maximumChangeCount, /* in */
            ref bool? maybeEnable   /* out */
            )
        {
            //
            // NOTE: Is there a minimum or maximum number of allowed changes
            //       (per time-interval) for this cache?
            //
            if ((minimumChangeCount >= 0) || (maximumChangeCount >= 0))
            {
                //
                // NOTE: Figure out how many milliseconds it has been since
                //       the change epoch.
                //
                double milliseconds = GetChangeEpochMilliseconds();

                if ((milliseconds != Milliseconds.Never) &&
                    (milliseconds > changeMilliseconds))
                {
                    try
                    {
                        //
                        // NOTE: If there are absolutely no changes, the
                        //       cache was previously reset and disabled.
                        //       Also, we know the configured interval of
                        //       time has elapsed; the caller should enable
                        //       the cache; otherwise, check if the number
                        //       of changes since the last reset fits the
                        //       criteria for disabling the cache specified
                        //       by the caller; in that case, the caller
                        //       should disable the cache.
                        //
                        if (changeCount == 0)
                        {
                            maybeEnable = true;
                        }
                        else
                        {
                            if (((minimumChangeCount < 0) ||
                                    (changeCount >= minimumChangeCount)) &&
                                ((maximumChangeCount < 0) ||
                                    (changeCount >= maximumChangeCount)))
                            {
                                maybeEnable = false;
                            }
                        }
                    }
                    finally
                    {
                        //
                        // NOTE: When this method is called, always reset
                        //       the change epoch and count.
                        //
                        TouchChangeEpochAndCount();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method trims excess elements from this dictionary, removing
        /// the least useful entries when the element count exceeds the
        /// configured maximum and enough time has elapsed since the last trim.
        /// </summary>
        /// <param name="minimumCount">
        /// The minimum number of elements that must remain in this dictionary
        /// after trimming, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of elements allowed in this dictionary before
        /// trimming occurs, or a negative value to disable trimming.
        /// </param>
        /// <param name="minimumRemoveCount">
        /// The minimum number of elements that must be eligible for removal
        /// before any are removed, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumRemoveCount">
        /// The maximum number of elements that may be removed in a single trim
        /// operation, or a negative value to impose no maximum.
        /// </param>
        /// <param name="minimumAccessCount">
        /// The minimum access count an element may have to be eligible for
        /// removal, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count an element may have to be eligible for
        /// removal, or a negative value to impose no maximum.
        /// </param>
        public virtual void TrimExcess( /* O(N) */
            int minimumCount,       /* in */
            int maximumCount,       /* in */
            int minimumRemoveCount, /* in */
            int maximumRemoveCount, /* in */
            int minimumAccessCount, /* in */
            int maximumAccessCount  /* in */
            )
        {
            int possibleRemoveCount = 0;

            TrimExcess(
                minimumCount, maximumCount, minimumRemoveCount,
                maximumRemoveCount, minimumAccessCount,
                maximumAccessCount, ref possibleRemoveCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method trims excess elements from this dictionary, removing
        /// the least useful entries when the element count exceeds the
        /// configured maximum and enough time has elapsed since the last trim,
        /// and reports how many elements could have been removed.
        /// </summary>
        /// <param name="minimumCount">
        /// The minimum number of elements that must remain in this dictionary
        /// after trimming, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumCount">
        /// The maximum number of elements allowed in this dictionary before
        /// trimming occurs, or a negative value to disable trimming.
        /// </param>
        /// <param name="minimumRemoveCount">
        /// The minimum number of elements that must be eligible for removal
        /// before any are removed, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumRemoveCount">
        /// The maximum number of elements that may be removed in a single trim
        /// operation, or a negative value to impose no maximum.
        /// </param>
        /// <param name="minimumAccessCount">
        /// The minimum access count an element may have to be eligible for
        /// removal, or a negative value to impose no minimum.
        /// </param>
        /// <param name="maximumAccessCount">
        /// The maximum access count an element may have to be eligible for
        /// removal, or a negative value to impose no maximum.
        /// </param>
        /// <param name="possibleRemoveCount">
        /// Upon return, this receives the number of elements that could have
        /// been removed to bring this dictionary within its configured
        /// maximum, or zero if no trimming was possible.
        /// </param>
        public virtual void TrimExcess( /* O(N) */
            int minimumCount,           /* in */
            int maximumCount,           /* in */
            int minimumRemoveCount,     /* in */
            int maximumRemoveCount,     /* in */
            int minimumAccessCount,     /* in */
            int maximumAccessCount,     /* in */
            ref int possibleRemoveCount /* out */
            )
        {
            //
            // NOTE: Initially, assume that we cannot do any trimming, due to
            //       the parameters specified by the caller; later, this may
            //       change.
            //
            possibleRemoveCount = 0;

            //
            // NOTE: Is there a maximum number of items configured for this
            //       cache?  If not, do nothing.
            //
            if (maximumCount >= 0)
            {
                //
                // NOTE: Grab the current number of items in this cache and see
                //       if it exceeds the configured maximum.
                //
                int beforeCount = this.Count;

                if (beforeCount > maximumCount)
                {
                    //
                    // NOTE: Figure out how many milliseconds it has been since
                    //       we last trimmed excess elements, if ever.
                    //
                    double milliseconds = GetLastTrimMilliseconds();

                    //
                    // NOTE: If we have never trimmed excess elements or it has
                    //       been longer than the configured time span, do it
                    //       again now.
                    //
                    if ((milliseconds == Milliseconds.Never) ||
                        (milliseconds > trimMilliseconds))
                    {
                        //
                        // NOTE: How many items need to be removed to fit
                        //       within the upper limit?
                        //
                        int removeCount = beforeCount - maximumCount;

                        //
                        // NOTE: We know that some trimming is possible;
                        //       if the correct parameters are specified
                        //       by the caller.
                        //
                        possibleRemoveCount = removeCount;

                        //
                        // NOTE: If there is no lower limit on the number
                        //       of items that should remain in the cache
                        //       -OR- the number of items that to remain
                        //       in the cache satisfies it, then proceed
                        //       with removing the items.
                        //
                        if (((minimumCount < 0) ||
                                ((beforeCount - removeCount) >= minimumCount)) &&
                            ((minimumRemoveCount < 0) ||
                                (removeCount >= minimumRemoveCount)))
                        {
                            //
                            // NOTE: If the number of items to be removed
                            //       exceeds the specified maximum, just
                            //       make it the specified maximum.
                            //
                            if ((maximumRemoveCount >= 0) &&
                                (removeCount > maximumRemoveCount))
                            {
                                removeCount = maximumRemoveCount;
                            }

                            //
                            // NOTE: Try to remove the specified number of
                            //       items.  This MAY have no effect -OR-
                            //       it may cause the removal of a different
                            //       number of items.
                            //
                            int foundCount = 0;
                            int removedCount = 0;

                            Remove(minimumAccessCount, maximumAccessCount,
                                removeCount, ref foundCount, ref removedCount);

                            ///////////////////////////////////////////////////

#if DEBUG || FORCE_TRACE
                            if (removedCount != removeCount)
                            {
                                int afterCount = this.Count;

                                TraceOps.DebugTrace(String.Format(
                                    "TrimExcess: minimumAccessCount = {0}, " +
                                    "maximumAccessCount = {1}, " +
                                    "removeCount = {2}, foundCount = {3}, " +
                                    "removedCount = {4}, beforeCount = {5}, " +
                                    "afterCount = {6}, trimCount = {7}",
                                    minimumAccessCount, maximumAccessCount,
                                    removeCount, foundCount, removedCount,
                                    beforeCount, afterCount, trimCount),
                                    GetType().Name, TracePriority.CacheDebug);
                            }
#endif

                            ///////////////////////////////////////////////////

                            //
                            // NOTE: Ok, we have successfully trimmed the
                            //       excess elements, record the current
                            //       time so that we will not do it again
                            //       too soon.
                            //
                            lastTrim = Now; lastTrimCount = removedCount;

                            //
                            // NOTE: Another trim operation was completed.
                            //
                            trimCount++;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
