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

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("8ac7601d-e0f4-406c-9812-f3af76831d89")]
    internal class CacheDictionary<TKey, TValue> :
            Dictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
        #region Private Constants
        private const double DefaultTrimMilliseconds = 60000.0; /* 1 min */
        private const double DefaultChangeMilliseconds = 30000.0; /* 30 secs */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private DateTime? lastTrim;
        private int lastTrimCount;
        private int trimCount;
        private double trimMilliseconds;

        ///////////////////////////////////////////////////////////////////////

        private DateTime? changeEpoch;
        private int changeCount;
        private double changeMilliseconds;

        ///////////////////////////////////////////////////////////////////////

        private int maximumCount;
        private int maximumAccessCount;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This dictionary is used to keep track of the last access
        //       time and usage count of each key in the base dictionary.
        //
        private Dictionary<TKey, DateTimeIntPair> accessed;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This dictionary is used to keep track of the "list" of
        //       keys from the base dictionary accessed at a particular
        //       point in time, sorted in order, from the oldest access
        //       time to the newest access time.
        //
        private SortedDictionary<
            DateTime?, Dictionary<TKey, object>> keysAccessed;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public CacheDictionary()
            : base()
        {
            Initialize();
        }

        ///////////////////////////////////////////////////////////////////////

        public CacheDictionary(
            IEqualityComparer<TKey> comparer
            )
            : base(comparer)
        {
            Initialize(0, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        public CacheDictionary(
            int capacity
            )
            : base(capacity)
        {
            Initialize(capacity, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        public CacheDictionary(
            int capacity,
            IEqualityComparer<TKey> comparer
            )
            : base(capacity, comparer)
        {
            Initialize(capacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        public CacheDictionary(
            IDictionary<TKey, TValue> dictionary
            )
            : base(dictionary)
        {
            Initialize();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void Initialize()
        {
            Initialize(0, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void UpdateMaximumCount()
        {
            int count = this.Count;

            if (count > maximumCount)
                maximumCount = count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Remove Helper Methods
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
        private void UpdateChangeCountAndMaybeTouchEpoch()
        {
            if (changeEpoch == null)
                changeEpoch = Now;

            changeCount++;
        }

        ///////////////////////////////////////////////////////////////////////

        private void TouchChangeEpochAndCount()
        {
            changeEpoch = Now;
            changeCount = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Iteration Helper Methods
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
                    return true;
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
        private static DateTime Now
        {
            get { return TimeOps.GetUtcNow(); }
        }

        ///////////////////////////////////////////////////////////////////////

        private void InitializeAccessed()
        {
            InitializeAccessed(0, this.Comparer);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void ResetSettings()
        {
            trimMilliseconds = DefaultTrimMilliseconds;
            changeMilliseconds = DefaultChangeMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        private void ResetStatistics()
        {
            lastTrim = null; lastTrimCount = 0; trimCount = 0;
            changeEpoch = null; changeCount = 0;

            maximumCount = 0;
            maximumAccessCount = 0;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Last Trim Helper Methods
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
        void ICollection<KeyValuePair<TKey, TValue>>.Add(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(
            KeyValuePair<TKey, TValue> item
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<TKey, TValue> Overrides
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

        bool IDictionary<TKey, TValue>.ContainsKey(
            TKey key
            )
        {
            UpdateAccessedAndCount(key, Now, 1, false);

            return base.ContainsKey(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<TKey, TValue>.Remove(
            TKey key
            )
        {
            RemoveAccessed(key, GetAccessed(key));
            UpdateChangeCountAndMaybeTouchEpoch();

            return base.Remove(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<TKey, TValue>.TryGetValue(
            TKey key,
            out TValue value
            )
        {
            UpdateAccessedAndCount(key, Now, 1, false);

            return base.TryGetValue(key, out value); /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<TKey, TValue> Overrides
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

        public virtual new bool ContainsKey(
            TKey key
            )
        {
            UpdateAccessedAndCount(key, Now, 1, false);

            return base.ContainsKey(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual new bool Remove(
            TKey key
            )
        {
            RemoveAccessed(key, GetAccessed(key));
            UpdateChangeCountAndMaybeTouchEpoch();

            return base.Remove(key); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual new bool TryGetValue(
            TKey key,
            out TValue value
            )
        {
            UpdateAccessedAndCount(key, Now, 1, false);

            return base.TryGetValue(key, out value); /* throw */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dictionary<TKey, TValue> Overrides
        public virtual new void Clear()
        {
            ClearAccessed(false);
            ResetStatistics();

            base.Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
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
        public virtual DateTime? LastTrim
        {
            get { return lastTrim; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int LastTrimCount
        {
            get { return lastTrimCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int TrimCount
        {
            get { return trimCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual double TrimMilliseconds
        {
            get { return trimMilliseconds; }
            set { trimMilliseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual DateTime? ChangeEpoch
        {
            get { return changeEpoch; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int ChangeCount
        {
            get { return changeCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual double ChangeMilliseconds
        {
            get { return changeMilliseconds; }
            set { changeMilliseconds = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int MaximumCount
        {
            get { return maximumCount; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int MaximumAccessCount
        {
            get { return maximumAccessCount; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual bool IsAccessedEnabled()
        {
            return (accessed != null) && (keysAccessed != null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public virtual void RestartChanges()
        {
            TouchChangeEpochAndCount();
        }

        ///////////////////////////////////////////////////////////////////////

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
