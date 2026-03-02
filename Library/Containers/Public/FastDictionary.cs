/*
 * FastDictionary.cs --
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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
using System.Runtime.CompilerServices;
#endif

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f7a3e1c9-6b2d-4d8e-9f05-3c7a8b1e4d62")]
    public class FastDictionary<TKey, TValue> :
            IDictionary<TKey, TValue>,
            IDictionary,
            ICollection<KeyValuePair<TKey, TValue>>,
            IEnumerable<KeyValuePair<TKey, TValue>>,
            IEnumerable, IReadOnly
#if SERIALIZATION
            , ISerializable
#endif
    {
        #region Private Constants
        //
        // NOTE: The default initial capacity for the hash table.  This
        //       must always be a power of two.
        //
        private const int DefaultCapacity = 16;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The maximum load factor before a resize is triggered.
        //       A value of 0.75 is the industry standard for open-
        //       addressing hash tables with Robin Hood hashing.
        //
        private const double DefaultLoadFactor = 0.75;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The distances array uses zero to indicate an empty slot.
        //       Occupied slots store (probeDistance + 1).  Therefore, the
        //       maximum representable probe distance is 254 (stored as
        //       255).
        //
        private const byte EmptyMarker = 0;
        private const byte MaxStoredDistance = 255;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: The synchronization object for this dictionary instance.
        //       All mutable operations must acquire this lock.
        //
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The equality comparer(s) used for key comparisons and
        //       hash code computation.
        //
        private IEqualityComparer<TKey> comparer;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Parallel arrays for the open-addressing hash table.
        //       Using parallel arrays rather than an array of structs
        //       keeps the hash codes contiguous in memory for cache-
        //       friendly probing during lookups.
        //
        //       The "distances" array serves double duty: a value of
        //       zero (EmptyMarker) indicates an empty slot; any non-
        //       zero value indicates an occupied slot with an actual
        //       probe distance of (distances[index] - 1).
        //
        private TKey[] keys;
        private TValue[] values;
        private int[] hashCodes;
        private byte[] distances;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of live entries currently in the hash table.
        //
        private int count;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The current capacity of the parallel arrays.  This is
        //       always a power of two.
        //
        private int capacity;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: A bitmask equal to (capacity - 1), used for fast modular
        //       arithmetic via bitwise AND instead of the modulo operator.
        //
        private int mask;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of entries at which a resize will be triggered.
        //       This is equal to (int)(capacity * DefaultLoadFactor).
        //
        private int threshold;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this field is non-zero, the entire dictionary instance
        //       is read-only and cannot be modified in any way.  Any attempt
        //       to modify read-only dictionary instances will result in an
        //       exception being thrown.
        //
        private bool isReadOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public FastDictionary()
        {
            Initialize(DefaultCapacity, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public FastDictionary(
            int capacity /* in */
            )
        {
            Initialize(capacity, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public FastDictionary(
            IEqualityComparer<TKey> comparer /* in */
            )
        {
            Initialize(DefaultCapacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        public FastDictionary(
            int capacity,                    /* in */
            IEqualityComparer<TKey> comparer /* in */
            )
        {
            Initialize(capacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        public FastDictionary(
            IDictionary<TKey, TValue> dictionary /* in */
            )
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            Initialize(
                Math.Max(
                    DefaultCapacity,
                    RoundUpToPowerOf2(dictionary.Count)),
                null);

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                InternalInsert(pair.Key, pair.Value, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public FastDictionary(
            IDictionary<TKey, TValue> dictionary, /* in */
            IEqualityComparer<TKey> comparer      /* in */
            )
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            Initialize(
                Math.Max(
                    DefaultCapacity,
                    RoundUpToPowerOf2(dictionary.Count)),
                comparer);

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                InternalInsert(pair.Key, pair.Value, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected FastDictionary(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
        {
            IEqualityComparer<TKey> serializedComparer =
                (IEqualityComparer<TKey>)info.GetValue(
                    "comparer", typeof(IEqualityComparer<TKey>));

            TKey[] serializedKeys = (TKey[])info.GetValue(
                "keys", typeof(TKey[]));

            TValue[] serializedValues = (TValue[])info.GetValue(
                "values", typeof(TValue[]));

            bool serializedReadOnly = info.GetBoolean("isReadOnly");

            int entryCount = (serializedKeys != null) ?
                serializedKeys.Length : 0;

            Initialize(
                Math.Max(
                    DefaultCapacity,
                    RoundUpToPowerOf2(entryCount)),
                serializedComparer);

            for (int index = 0; index < entryCount; index++)
                InternalInsert(serializedKeys[index],
                    serializedValues[index], true);

            isReadOnly = serializedReadOnly;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int RoundUpToPowerOf2(
            int value /* in */
            )
        {
            if (value < DefaultCapacity)
                return DefaultCapacity;

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Murmur3-style bit mixing finalizer.  This distributes
        //       poorly-mixed hash codes more evenly across the table,
        //       reducing clustering.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int MixHashCode(
            int hashCode /* in */
            )
        {
            unchecked
            {
                hashCode ^= (int)((uint)hashCode >> 16);
                hashCode *= (int)0x85ebca6b;
                hashCode ^= (int)((uint)hashCode >> 13);
                hashCode *= (int)0xc2b2ae35;
                hashCode ^= (int)((uint)hashCode >> 16);
            }

            //
            // NOTE: Ensure the hash code is non-negative.
            //
            return hashCode & 0x7FFFFFFF;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void Initialize(
            int requestedCapacity,                    /* in */
            IEqualityComparer<TKey> requestedComparer /* in */
            )
        {
            capacity = RoundUpToPowerOf2(
                Math.Max(requestedCapacity, DefaultCapacity));

            mask = capacity - 1;
            threshold = (int)(capacity * DefaultLoadFactor);

            keys = new TKey[capacity];
            values = new TValue[capacity];
            hashCodes = new int[capacity];
            distances = new byte[capacity];

            count = 0;

            if (requestedComparer != null)
                comparer = requestedComparer;
            else
                comparer = EqualityComparer<TKey>.Default;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int ComputeHashCode(
            TKey key /* in */
            )
        {
            return MixHashCode(comparer.GetHashCode(key));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Returns true if the slot at the given index is occupied.
        //       A distance value of EmptyMarker (zero) indicates an empty
        //       slot.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool IsOccupied(
            int index /* in */
            )
        {
            return distances[index] != EmptyMarker;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Returns the actual probe distance for an occupied slot.
        //       The stored value is (probeDistance + 1) because zero is
        //       reserved as the empty marker.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private byte GetDistance(
            int index /* in */
            )
        {
            return (byte)(distances[index] - 1);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Stores the probe distance for a slot, adding 1 to
        //       distinguish it from the empty marker.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void SetDistance(
            int index,    /* in */
            byte distance /* in */
            )
        {
            distances[index] = (byte)(distance + 1);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Marks a slot as empty by setting its distance to the
        //       empty marker and clearing the key and value to allow
        //       garbage collection of reference types.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void ClearSlot(
            int index /* in */
            )
        {
            keys[index] = default(TKey);
            values[index] = default(TValue);
            hashCodes[index] = 0;
            distances[index] = EmptyMarker;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Core Robin Hood lookup.  Probes linearly from the ideal
        //       slot, short-circuiting when the current probe distance
        //       exceeds the stored distance (Robin Hood invariant).
        //
        //       Returns a non-negative index if the key is found, or a
        //       negative (bitwise complement) index if not found.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int FindSlot(
            TKey key,    /* in */
            int hashCode /* in */
            )
        {
            int index = hashCode & mask;
            byte distance = 0;

            while (true)
            {
                if (!IsOccupied(index))
                    return ~index; // empty slot, key not found

                if (distance > GetDistance(index))
                    return ~index; // Robin Hood: key cannot exist further

                if ((hashCodes[index] == hashCode) &&
                    comparer.Equals(keys[index], key))
                {
                    return index; // found
                }

                index = (index + 1) & mask;
                distance++;

                if (distance >= MaxStoredDistance)
                    return ~index; // safety: should not happen
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Robin Hood insertion.  Places the entry at the ideal slot
        //       or steals a slot from a "richer" (closer-to-home) entry,
        //       displacing it further along the probe chain.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void InternalInsert(
            TKey key,             /* in */
            TValue value,         /* in */
            bool throwOnDuplicate /* in */
            )
        {
            int hashCode = ComputeHashCode(key);
            int index = hashCode & mask;
            byte distance = 0;

            while (true)
            {
                if (!IsOccupied(index))
                {
                    //
                    // NOTE: Empty slot found; place the entry here.
                    //
                    keys[index] = key;
                    values[index] = value;
                    hashCodes[index] = hashCode;

                    SetDistance(index, distance);

                    count++;

                    if (count >= threshold)
                        Resize();

                    return;
                }

                if ((hashCodes[index] == hashCode) &&
                    comparer.Equals(keys[index], key))
                {
                    //
                    // NOTE: Key already exists.
                    //
                    if (throwOnDuplicate)
                    {
                        throw new ArgumentException(
                            "An item with the same key has already " +
                            "been added.");
                    }

                    values[index] = value;
                    return;
                }

                //
                // NOTE: Robin Hood swap: if our probe distance exceeds
                //       the existing entry's distance, steal this slot
                //       and continue inserting the displaced entry.
                //
                if (distance > GetDistance(index))
                {
                    //
                    // NOTE: Swap the new entry with the existing one.
                    //
                    TKey tempKey = keys[index];

                    keys[index] = key;
                    key = tempKey;

                    TValue tempValue = values[index];

                    values[index] = value;
                    value = tempValue;

                    int tempHashCode = hashCodes[index];

                    hashCodes[index] = hashCode;
                    hashCode = tempHashCode;

                    byte tempDistance = GetDistance(index);

                    SetDistance(index, distance);
                    distance = tempDistance;
                }

                index = (index + 1) & mask;
                distance++;

                //
                // NOTE: Safety check for extreme probe distance.
                //       This should never happen with a reasonable
                //       load factor, but guard against infinite loops.
                //
                if (distance >= MaxStoredDistance)
                {
                    Resize();

                    //
                    // NOTE: After resize, re-insert the displaced
                    //       entry from scratch since all indices
                    //       have changed.
                    //
                    InternalInsert(key, value, false);
                    return;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Backward-shift deletion.  Removes the entry at the given
        //       index and shifts subsequent displaced entries backward to
        //       fill the gap, maintaining the Robin Hood invariant without
        //       tombstones.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void InternalRemoveAt(
            int index /* in */
            )
        {
            count--;

            //
            // NOTE: Shift subsequent displaced entries backward to fill
            //       the gap.  Stop when an empty slot is reached or an
            //       entry at its ideal position (distance == 0) is found.
            //
            int next = (index + 1) & mask;

            while (IsOccupied(next) && (GetDistance(next) > 0))
            {
                //
                // NOTE: Move the entry at 'next' backward to 'index'.
                //
                keys[index] = keys[next];
                values[index] = values[next];
                hashCodes[index] = hashCodes[next];
                SetDistance(index, (byte)(GetDistance(next) - 1));

                //
                // NOTE: Advance to the next slot.
                //
                index = next;
                next = (next + 1) & mask;
            }

            //
            // NOTE: Clear the final vacated slot to allow garbage
            //       collection of reference types.
            //
            ClearSlot(index);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Doubles the capacity of the hash table and re-inserts all
        //       existing entries.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Resize()
        {
            TKey[] oldKeys = keys;
            TValue[] oldValues = values;
            byte[] oldDistances = distances;
            int oldCapacity = capacity;

            int newCapacity = capacity * 2;

            if (newCapacity < 0)
            {
                //
                // NOTE: Integer overflow; cannot resize further.
                //
                throw new InvalidOperationException(
                    "hash table has exceeded maximum capacity");
            }

            capacity = newCapacity;
            mask = capacity - 1;
            threshold = (int)(capacity * DefaultLoadFactor);

            keys = new TKey[capacity];
            values = new TValue[capacity];
            hashCodes = new int[capacity];
            distances = new byte[capacity];

            int savedCount = count;

            count = 0;

            for (int index = 0; index < oldCapacity; index++)
            {
                if (oldDistances[index] != EmptyMarker)
                {
                    InternalInsert(
                    oldKeys[index], oldValues[index], false);
                }
            }

            //
            // NOTE: Verify that all entries were re-inserted.  This is
            //       a sanity check; it should always pass.
            //
            if (count != savedCount)
            {
                throw new InvalidOperationException(String.Format(
                    "entry count mismatch after resize: expected {0}, " +
                    "got {1}", savedCount, count));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Returns a snapshot of all live entries as an array.  This
        //       is used by the enumerator and CopyTo methods.
        //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private KeyValuePair<TKey, TValue>[] InternalToArray()
        {
            KeyValuePair<TKey, TValue>[] result =
                new KeyValuePair<TKey, TValue>[count];

            int index2 = 0;

            for (int index = 0; index < capacity; index++)
            {
                if (IsOccupied(index))
                {
                    result[index2++] = new KeyValuePair<TKey, TValue>(
                        keys[index], values[index]);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool InternalTryGetValue(
            TKey key,        /* in */
            out TValue value /* in */
            )
        {
            int hashCode = ComputeHashCode(key);
            int index = FindSlot(key, hashCode);

            if (index >= 0)
            {
                value = values[index];
                return true;
            }

            value = default(TValue);
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckReadOnly()
        {
            if (isReadOnly)
                throw new ScriptException("dictionary is read-only");
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private void MakeReadOnly()
        {
            lock (syncRoot)
            {
                isReadOnly = true;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private TValue GetValue(
            TKey key /* in */
            )
        {
            int hashCode = ComputeHashCode(key);
            int index = FindSlot(key, hashCode);

            if (index >= 0)
                return values[index];

            throw new KeyNotFoundException(String.Format(
                "The given key \"{0}\" was not present in " +
                "the dictionary.", key));
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private bool ComputeHashCodeAndFindSlot(
            TKey key /* in */
            )
        {
            int hashCode = ComputeHashCode(key);

            return FindSlot(key, hashCode) >= 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<TKey, TValue> Members
        public virtual TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                if (isReadOnly)
                {
                    return GetValue(key);
                }
                else
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        return GetValue(key);
                    }
                }
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CheckReadOnly();

                    InternalInsert(key, value, false);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IAnyCollection<TKey> Keys
        {
            get
            {
                if (isReadOnly)
                {
                    return InternalGetKeyCollection();
                }
                else
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        return InternalGetKeyCollection();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual IAnyCollection<TValue> Values
        {
            get
            {
                if (isReadOnly)
                {
                    return InternalGetValueCollection();
                }
                else
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        return InternalGetValueCollection();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return Keys; }
        }

        ///////////////////////////////////////////////////////////////////////

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return Values; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Add(
            TKey key,    /* in */
            TValue value /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            lock (syncRoot) /* TRANSACTIONAL */
            {
                CheckReadOnly();

                InternalInsert(key, value, true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ContainsKey(
            TKey key /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (isReadOnly)
            {
                return ComputeHashCodeAndFindSlot(key);
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return ComputeHashCodeAndFindSlot(key);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Remove(
            TKey key /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            lock (syncRoot) /* TRANSACTIONAL */
            {
                CheckReadOnly();

                int hashCode = ComputeHashCode(key);
                int index = FindSlot(key, hashCode);

                if (index < 0)
                    return false;

                InternalRemoveAt(index);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool TryGetValue(
            TKey key,        /* in */
            out TValue value /* out */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (isReadOnly)
            {
                return InternalTryGetValue(key, out value);
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return InternalTryGetValue(key, out value);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection<KeyValuePair<TKey, TValue>> Members
        public virtual int Count
        {
            get
            {
                if (isReadOnly)
                {
                    return count;
                }
                else
                {
                    lock (syncRoot)
                    {
                        return count;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return IsReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Add(
            KeyValuePair<TKey, TValue> item /* in */
            )
        {
            Add(item.Key, item.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Clear()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                CheckReadOnly();

                Array.Clear(keys, 0, capacity);
                Array.Clear(values, 0, capacity);
                Array.Clear(hashCodes, 0, capacity);
                Array.Clear(distances, 0, capacity);

                count = 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Contains(
            KeyValuePair<TKey, TValue> item /* in */
            )
        {
            TValue value;

            if (!TryGetValue(item.Key, out value))
                return false;

            return EqualityComparer<TValue>.Default.Equals(
                value, item.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void CopyTo(
            KeyValuePair<TKey, TValue>[] array, /* out */
            int arrayIndex                      /* in */
            )
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");

            KeyValuePair<TKey, TValue>[] snapshot;

            if (isReadOnly)
            {
                snapshot = InternalToArray();
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    snapshot = InternalToArray();
                }
            }

            int snapshotLength = snapshot.Length;
            int arrayLength = array.Length;

            if (arrayIndex + snapshotLength > arrayLength)
            {
                throw new ArgumentException(
                    "Destination array is not long enough.");
            }

            Array.Copy(snapshot, 0, array, arrayIndex, snapshotLength);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Remove(
            KeyValuePair<TKey, TValue> item /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                CheckReadOnly();

                int hashCode = ComputeHashCode(item.Key);
                int index = FindSlot(item.Key, hashCode);

                if (index < 0)
                    return false;

                if (!EqualityComparer<TValue>.Default.Equals(
                        values[index], item.Value))
                {
                    return false;
                }

                InternalRemoveAt(index);
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerable<KeyValuePair<TKey, TValue>> Members
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            KeyValuePair<TKey, TValue>[] snapshot;

            if (isReadOnly)
            {
                snapshot = InternalToArray();
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    snapshot = InternalToArray();
                }
            }

            return ((IEnumerable<KeyValuePair<TKey, TValue>>)snapshot)
                .GetEnumerator();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadOnly Members
        public virtual bool IsReadOnly
        {
            get { return isReadOnly; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary Members
        private static bool IsCompatibleKey(
            object key /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return key is TKey;
        }

        ///////////////////////////////////////////////////////////////////////

        object IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    TValue value;

                    if (TryGetValue((TKey)key, out value))
                        return value;
                }

                return null;
            }
            set
            {
                this[(TKey)key] = (TValue)value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        ICollection IDictionary.Keys
        {
            get
            {
                if (isReadOnly)
                {
                    return InternalGetKeyCollection();
                }
                else
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        return InternalGetKeyCollection();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        ICollection IDictionary.Values
        {
            get
            {
                if (isReadOnly)
                {
                    return InternalGetValueCollection();
                }
                else
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        return InternalGetValueCollection();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary.IsFixedSize
        {
            get { return isReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary.IsReadOnly
        {
            get { return isReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary.Contains(
            object key /* in */
            )
        {
            if (IsCompatibleKey(key))
                return ContainsKey((TKey)key);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary.Add(
            object key,  /* in */
            object value /* in */
            )
        {
            Add((TKey)key, (TValue)value);
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary.Remove(
            object key /* in */
            )
        {
            if (IsCompatibleKey(key))
                Remove((TKey)key);
        }

        ///////////////////////////////////////////////////////////////////////

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            KeyValuePair<TKey, TValue>[] snapshot;

            if (isReadOnly)
            {
                snapshot = InternalToArray();
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    snapshot = InternalToArray();
                }
            }

            return new FastDictionaryEnumerator(snapshot);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection Members (Non-Generic)
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        object ICollection.SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        void ICollection.CopyTo(
            Array array,   /* out */
            int arrayIndex /* in */
            )
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");

            KeyValuePair<TKey, TValue>[] snapshot;

            if (isReadOnly)
            {
                snapshot = InternalToArray();
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    snapshot = InternalToArray();
                }
            }

            int snapshotLength = snapshot.Length;
            int arrayLength = array.Length;

            if (arrayIndex + snapshotLength > arrayLength)
            {
                throw new ArgumentException(
                    "Destination array is not long enough.");
            }

            for (int index = 0; index < snapshotLength; index++)
            {
                array.SetValue(
                    new DictionaryEntry(
                        snapshot[index].Key,
                        snapshot[index].Value),
                    arrayIndex + index);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region FastKeyCollection Class
        [ObjectId("950a2f1c-faaa-40da-a58b-0fbfe07f1b0c")]
        public sealed class FastKeyCollection :
                List<TKey>, IAnyCollection<TKey>
        {
            #region Public Constructors
            public FastKeyCollection(
                IEnumerable<TKey> collection /* in */
                )
                : base(collection)
            {
                // do nothing.
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region FastValueCollection Class
        [ObjectId("79dd8044-55c7-466d-aa8e-294a120c6cda")]
        public sealed class FastValueCollection :
                List<TValue>, IAnyCollection<TValue>
        {
            #region Public Constructors
            public FastValueCollection(
                IEnumerable<TValue> collection /* in */
                )
                : base(collection)
            {
                // do nothing.
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region FastDictionaryEnumerator Class
        //
        // NOTE: Snapshot-based IDictionaryEnumerator implementation for
        //       the non-generic IDictionary.GetEnumerator() method.
        //
        [ObjectId("1e1f9f28-4593-45bd-bbc7-f86df4babc2b")]
        private sealed class FastDictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly KeyValuePair<TKey, TValue>[] snapshot;
            private int position;

            ///////////////////////////////////////////////////////////////////

            internal FastDictionaryEnumerator(
                KeyValuePair<TKey, TValue>[] snapshot /* in */
                )
            {
                this.snapshot = snapshot;
                this.position = -1;
            }

            ///////////////////////////////////////////////////////////////////

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(
                        snapshot[position].Key,
                        snapshot[position].Value);
                }
            }

            ///////////////////////////////////////////////////////////////////

            public object Key
            {
                get { return snapshot[position].Key; }
            }

            ///////////////////////////////////////////////////////////////////

            public object Value
            {
                get { return snapshot[position].Value; }
            }

            ///////////////////////////////////////////////////////////////////

            public object Current
            {
                get { return Entry; }
            }

            ///////////////////////////////////////////////////////////////////

            public bool MoveNext()
            {
                if (position < snapshot.Length - 1)
                {
                    position++;
                    return true;
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            public void Reset()
            {
                position = -1;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public virtual IEqualityComparer<TKey> Comparer
        {
            get { return comparer; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual bool TryAdd(
            TKey key,    /* in */
            TValue value /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            lock (syncRoot) /* TRANSACTIONAL */
            {
                CheckReadOnly();

                int hashCode = ComputeHashCode(key);
                int index = FindSlot(key, hashCode);

                if (index >= 0)
                    return false; // key already exists

                InternalInsert(key, value, false);
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool ContainsValue(
            TValue value /* in */
            )
        {
            if (isReadOnly)
            {
                return InternalContainsValue(value);
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return InternalContainsValue(value);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private TKey[] InternalGetKeys()
        {
            TKey[] result = new TKey[count];
            int index2 = 0;

            for (int index = 0; index < capacity; index++)
            {
                if (IsOccupied(index))
                {
                    result[index2++] = keys[index];
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private TValue[] InternalGetValues()
        {
            TValue[] result = new TValue[count];
            int index2 = 0;

            for (int index = 0; index < capacity; index++)
            {
                if (IsOccupied(index))
                {
                    result[index2++] = values[index];
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private IAnyCollection<TKey> InternalGetKeyCollection()
        {
            return new FastKeyCollection(InternalGetKeys());
        }

        ///////////////////////////////////////////////////////////////////////

        private IAnyCollection<TValue> InternalGetValueCollection()
        {
            return new FastValueCollection(InternalGetValues());
        }

        ///////////////////////////////////////////////////////////////////////

        private bool InternalContainsValue(
            TValue value /* in */
            )
        {
            IEqualityComparer<TValue> valueComparer =
                EqualityComparer<TValue>.Default;

            for (int index = 0; index < capacity; index++)
            {
                if (IsOccupied(index) &&
                    valueComparer.Equals(values[index], value))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(
            SerializationInfo info,  /* in */
            StreamingContext context /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                info.AddValue("comparer", comparer,
                    typeof(IEqualityComparer<TKey>));

                info.AddValue("keys", InternalGetKeys(),
                    typeof(TKey[]));

                info.AddValue("values", InternalGetValues(),
                    typeof(TValue[]));

                info.AddValue("isReadOnly", isReadOnly);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            KeyValuePair<TKey, TValue>[] snapshot;

            if (isReadOnly)
            {
                snapshot = InternalToArray();
            }
            else
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    snapshot = InternalToArray();
                }
            }

            int snapshotLength = snapshot.Length;

            if (snapshotLength == 0)
                return String.Empty;

            StringBuilder builder = StringBuilderFactory.Create();

            for (int index = 0; index < snapshotLength; index++)
            {
                if (index > 0)
                    builder.Append(Characters.Space);

                builder.Append(snapshot[index].Key);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion
    }
}
