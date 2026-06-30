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
    /// <summary>
    /// This class represents a generic dictionary that maps keys to values
    /// using an open-addressing hash table with Robin Hood hashing and
    /// backward-shift deletion.  It stores its entries in parallel arrays for
    /// cache-friendly probing and supports an optional read-only mode that
    /// prevents any further modification.  In addition to the generic
    /// dictionary interfaces, it implements the non-generic dictionary and
    /// collection interfaces and, when serialization is enabled, supports
    /// custom serialization.
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
        /// <summary>
        /// The default initial capacity for the hash table, which must always
        /// be a power of two.
        /// </summary>
        private const int DefaultCapacity = 16;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The maximum load factor before a resize is triggered.
        //       A value of 0.75 is the industry standard for open-
        //       addressing hash tables with Robin Hood hashing.
        //
        /// <summary>
        /// The maximum load factor before a resize is triggered.
        /// </summary>
        private const double DefaultLoadFactor = 0.75;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The distances array uses zero to indicate an empty slot.
        //       Occupied slots store (probeDistance + 1).  Therefore, the
        //       maximum representable probe distance is 254 (stored as
        //       255).
        //
        /// <summary>
        /// The distance value that marks a slot as empty.
        /// </summary>
        private const byte EmptyMarker = 0;

        /// <summary>
        /// The maximum probe distance value that can be stored in the
        /// distances array.
        /// </summary>
        private const byte MaxStoredDistance = 255;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: The synchronization object for this dictionary instance.
        //       All mutable operations must acquire this lock.
        //
        /// <summary>
        /// The synchronization object for this dictionary instance.  All
        /// mutable operations must acquire this lock.
        /// </summary>
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The equality comparer(s) used for key comparisons and
        //       hash code computation.
        //
        /// <summary>
        /// The equality comparer used for key comparisons and hash code
        /// computation.
        /// </summary>
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
        /// <summary>
        /// The parallel array holding the keys for each occupied slot.
        /// </summary>
        private TKey[] keys;

        /// <summary>
        /// The parallel array holding the values for each occupied slot.
        /// </summary>
        private TValue[] values;

        /// <summary>
        /// The parallel array holding the mixed hash code of the key for each
        /// occupied slot.
        /// </summary>
        private int[] hashCodes;

        /// <summary>
        /// The parallel array holding the encoded probe distance for each
        /// slot, where a value of zero indicates an empty slot and any
        /// non-zero value indicates an occupied slot with an actual probe
        /// distance one less than the stored value.
        /// </summary>
        private byte[] distances;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of live entries currently in the hash table.
        //
        /// <summary>
        /// The number of live entries currently in the hash table.
        /// </summary>
        private int count;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The current capacity of the parallel arrays.  This is
        //       always a power of two.
        //
        /// <summary>
        /// The current capacity of the parallel arrays, which is always a
        /// power of two.
        /// </summary>
        private int capacity;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: A bitmask equal to (capacity - 1), used for fast modular
        //       arithmetic via bitwise AND instead of the modulo operator.
        //
        /// <summary>
        /// A bitmask equal to the capacity minus one, used for fast modular
        /// arithmetic via bitwise AND instead of the modulo operator.
        /// </summary>
        private int mask;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of entries at which a resize will be triggered.
        //       This is equal to (int)(capacity * DefaultLoadFactor).
        //
        /// <summary>
        /// The number of entries at which a resize will be triggered, equal to
        /// the capacity multiplied by the load factor.
        /// </summary>
        private int threshold;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this field is non-zero, the entire dictionary instance
        //       is read-only and cannot be modified in any way.  Any attempt
        //       to modify read-only dictionary instances will result in an
        //       exception being thrown.
        //
        /// <summary>
        /// When non-zero, the entire dictionary instance is read-only and
        /// cannot be modified in any way; any attempt to modify it will result
        /// in an exception being thrown.
        /// </summary>
        private bool isReadOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary using the default initial capacity
        /// and the default equality comparer for the key type.
        /// </summary>
        public FastDictionary()
        {
            Initialize(DefaultCapacity, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty dictionary using the specified initial capacity
        /// and the default equality comparer for the key type.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity for the hash table.  The actual capacity used
        /// will be rounded up to a power of two no smaller than the default
        /// initial capacity.
        /// </param>
        public FastDictionary(
            int capacity /* in */
            )
        {
            Initialize(capacity, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty dictionary using the default initial capacity
        /// and the specified equality comparer for the key type.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer to use for key comparisons and hash code
        /// computation.  If this parameter is null, the default equality
        /// comparer for the key type is used.
        /// </param>
        public FastDictionary(
            IEqualityComparer<TKey> comparer /* in */
            )
        {
            Initialize(DefaultCapacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty dictionary using the specified initial capacity
        /// and the specified equality comparer for the key type.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity for the hash table.  The actual capacity used
        /// will be rounded up to a power of two no smaller than the default
        /// initial capacity.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use for key comparisons and hash code
        /// computation.  If this parameter is null, the default equality
        /// comparer for the key type is used.
        /// </param>
        public FastDictionary(
            int capacity,                    /* in */
            IEqualityComparer<TKey> comparer /* in */
            )
        {
            Initialize(capacity, comparer);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary that contains a copy of the elements from
        /// the specified dictionary, using the default equality comparer for
        /// the key type.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// This parameter may not be null.
        /// </param>
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

        /// <summary>
        /// Constructs a dictionary that contains a copy of the elements from
        /// the specified dictionary, using the specified equality comparer for
        /// the key type.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// This parameter may not be null.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer to use for key comparisons and hash code
        /// computation.  If this parameter is null, the default equality
        /// comparer for the key type is used.
        /// </param>
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
        /// <summary>
        /// Constructs a dictionary from previously serialized data.  This
        /// constructor is used during deserialization to repopulate the
        /// dictionary, including its read-only state.
        /// </summary>
        /// <param name="info">
        /// The serialization information from which to read the serialized
        /// state of the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
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
        /// <summary>
        /// This method rounds the specified value up to the nearest power of
        /// two that is no smaller than the default initial capacity.
        /// </summary>
        /// <param name="value">
        /// The value to round up to a power of two.
        /// </param>
        /// <returns>
        /// The smallest power of two that is greater than or equal to both the
        /// specified value and the default initial capacity.
        /// </returns>
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
        /// <summary>
        /// This method applies a Murmur3-style bit mixing finalizer to the
        /// specified hash code, distributing poorly-mixed hash codes more
        /// evenly across the table to reduce clustering, and forces the result
        /// to be non-negative.
        /// </summary>
        /// <param name="hashCode">
        /// The raw hash code to mix.
        /// </param>
        /// <returns>
        /// The mixed, non-negative hash code.
        /// </returns>
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
        /// <summary>
        /// This method initializes the internal state of the dictionary,
        /// allocating the parallel arrays and establishing the capacity, mask,
        /// threshold, and equality comparer.
        /// </summary>
        /// <param name="requestedCapacity">
        /// The requested initial capacity for the hash table.  The actual
        /// capacity used will be rounded up to a power of two no smaller than
        /// the default initial capacity.
        /// </param>
        /// <param name="requestedComparer">
        /// The equality comparer to use for key comparisons and hash code
        /// computation.  If this parameter is null, the default equality
        /// comparer for the key type is used.
        /// </param>
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

        /// <summary>
        /// This method computes the mixed hash code for the specified key
        /// using the configured equality comparer.
        /// </summary>
        /// <param name="key">
        /// The key whose hash code is computed.
        /// </param>
        /// <returns>
        /// The mixed, non-negative hash code for the specified key.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the slot at the specified index is
        /// occupied.
        /// </summary>
        /// <param name="index">
        /// The index of the slot to test.
        /// </param>
        /// <returns>
        /// True if the slot is occupied; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method returns the actual probe distance for the occupied slot
        /// at the specified index, decoding the stored value that is offset by
        /// one to distinguish occupied slots from the empty marker.
        /// </summary>
        /// <param name="index">
        /// The index of the occupied slot whose probe distance is returned.
        /// </param>
        /// <returns>
        /// The actual probe distance for the specified slot.
        /// </returns>
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
        /// <summary>
        /// This method stores the actual probe distance for the slot at the
        /// specified index, encoding the value with an offset of one to
        /// distinguish occupied slots from the empty marker.
        /// </summary>
        /// <param name="index">
        /// The index of the slot whose probe distance is stored.
        /// </param>
        /// <param name="distance">
        /// The actual probe distance to store for the specified slot.
        /// </param>
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
        /// <summary>
        /// This method marks the slot at the specified index as empty,
        /// clearing its key, value, and hash code to allow garbage collection
        /// of reference types.
        /// </summary>
        /// <param name="index">
        /// The index of the slot to clear.
        /// </param>
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
        /// <summary>
        /// This method performs the core Robin Hood lookup, probing linearly
        /// from the ideal slot and short-circuiting when the current probe
        /// distance exceeds the stored distance.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <param name="hashCode">
        /// The previously computed mixed hash code for the key.
        /// </param>
        /// <returns>
        /// A non-negative index of the slot containing the key if it is found;
        /// otherwise, the bitwise complement of the index of the first empty
        /// or short-circuiting slot encountered.
        /// </returns>
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
        /// <summary>
        /// This method performs a Robin Hood insertion of the specified key
        /// and value, placing the entry at its ideal slot or stealing a slot
        /// from a closer-to-home entry and displacing it further along the
        /// probe chain.  If the key already exists, its value is updated unless
        /// duplicates are not permitted.  A resize is triggered when the load
        /// threshold is reached or the maximum probe distance is exceeded.
        /// </summary>
        /// <param name="key">
        /// The key to insert.
        /// </param>
        /// <param name="value">
        /// The value to associate with the key.
        /// </param>
        /// <param name="throwOnDuplicate">
        /// Non-zero to throw an exception if the key already exists; zero to
        /// overwrite the existing value instead.
        /// </param>
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
        /// <summary>
        /// This method performs a backward-shift deletion, removing the entry
        /// at the specified index and shifting subsequent displaced entries
        /// backward to fill the gap, maintaining the Robin Hood invariant
        /// without tombstones.
        /// </summary>
        /// <param name="index">
        /// The index of the occupied slot to remove.
        /// </param>
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
        /// <summary>
        /// This method doubles the capacity of the hash table, allocates new
        /// parallel arrays, and re-inserts all existing entries.  It throws an
        /// exception if the capacity cannot be increased further or if the
        /// entry count does not match after re-insertion.
        /// </summary>
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
        /// <summary>
        /// This method returns a snapshot of all live entries in the
        /// dictionary as an array of key/value pairs.
        /// </summary>
        /// <returns>
        /// An array containing a snapshot of all live entries in the
        /// dictionary.
        /// </returns>
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

        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// key without acquiring the synchronization lock.
        /// </summary>
        /// <param name="key">
        /// The key whose value is retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the key.  Upon
        /// failure, receives the default value for the value type.
        /// </param>
        /// <returns>
        /// True if the key was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method throws an exception if the dictionary is read-only.  It
        /// is called before any operation that would modify the dictionary.
        /// </summary>
        private void CheckReadOnly()
        {
            if (isReadOnly)
                throw new ScriptException("dictionary is read-only");
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method marks the dictionary as read-only, preventing any
        /// further modification.
        /// </summary>
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

        /// <summary>
        /// This method gets the value associated with the specified key
        /// without acquiring the synchronization lock, throwing an exception if
        /// the key is not present.
        /// </summary>
        /// <param name="key">
        /// The key whose value is retrieved.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// This method computes the hash code for the specified key and
        /// determines whether a slot containing the key exists in the hash
        /// table.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <returns>
        /// True if the key was found; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets or sets the value associated with the specified key.  Getting
        /// the value throws an exception if the key is not present; setting the
        /// value adds the key if it is not already present or overwrites the
        /// existing value otherwise.
        /// </summary>
        /// <param name="key">
        /// The key whose value is retrieved or set.  This parameter may not be
        /// null.
        /// </param>
        /// <returns>
        /// The value associated with the specified key.
        /// </returns>
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

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return Keys; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return Values; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified key and value to the dictionary,
        /// throwing an exception if the key already exists.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.  This parameter may not be null.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
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

        /// <summary>
        /// This method determines whether the dictionary contains the
        /// specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate.  This parameter may not be null.
        /// </param>
        /// <returns>
        /// True if the dictionary contains the specified key; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method removes the element with the specified key from the
        /// dictionary.
        /// </summary>
        /// <param name="key">
        /// The key of the element to remove.  This parameter may not be null.
        /// </param>
        /// <returns>
        /// True if the element was found and removed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// key.
        /// </summary>
        /// <param name="key">
        /// The key whose value is retrieved.  This parameter may not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the key.  Upon
        /// failure, receives the default value for the value type.
        /// </param>
        /// <returns>
        /// True if the key was found; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return IsReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified key/value pair to the dictionary,
        /// throwing an exception if the key already exists.
        /// </summary>
        /// <param name="item">
        /// The key/value pair to add.
        /// </param>
        public virtual void Add(
            KeyValuePair<TKey, TValue> item /* in */
            )
        {
            Add(item.Key, item.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all key/value pairs from the dictionary.
        /// </summary>
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

        /// <summary>
        /// This method determines whether the dictionary contains the
        /// specified key/value pair, matching both the key and the value.
        /// </summary>
        /// <param name="item">
        /// The key/value pair to locate.
        /// </param>
        /// <returns>
        /// True if the dictionary contains the specified key/value pair;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method copies a snapshot of the dictionary's key/value pairs to
        /// the specified array, starting at the specified index.
        /// </summary>
        /// <param name="array">
        /// The destination array that receives the key/value pairs.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in the destination array at which copying
        /// begins.
        /// </param>
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

        /// <summary>
        /// This method removes the specified key/value pair from the
        /// dictionary, matching both the key and the value.
        /// </summary>
        /// <param name="item">
        /// The key/value pair to remove.
        /// </param>
        /// <returns>
        /// True if the key/value pair was found and removed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method returns an enumerator that iterates over a snapshot of
        /// the key/value pairs in the dictionary.
        /// </summary>
        /// <returns>
        /// An enumerator over a snapshot of the key/value pairs in the
        /// dictionary.
        /// </returns>
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
        /// <summary>
        /// This method returns a non-generic enumerator that iterates over a
        /// snapshot of the key/value pairs in the dictionary.
        /// </summary>
        /// <returns>
        /// A non-generic enumerator over a snapshot of the key/value pairs in
        /// the dictionary.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IReadOnly Members
        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        public virtual bool IsReadOnly
        {
            get { return isReadOnly; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary Members
        /// <summary>
        /// This method determines whether the specified object is compatible
        /// with the key type of the dictionary.
        /// </summary>
        /// <param name="key">
        /// The candidate key object to test.  This parameter may not be null.
        /// </param>
        /// <returns>
        /// True if the specified object is an instance of the key type;
        /// otherwise, false.
        /// </returns>
        private static bool IsCompatibleKey(
            object key /* in */
            )
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return key is TKey;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the value associated with the specified key.  Getting
        /// the value returns null if the key is not present or is not
        /// compatible with the key type; setting the value adds the key if it
        /// is not already present or overwrites the existing value otherwise.
        /// </summary>
        /// <param name="key">
        /// The key whose value is retrieved or set.
        /// </param>
        /// <returns>
        /// The value associated with the specified key, or null if the key is
        /// not present.
        /// </returns>
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

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the dictionary has a fixed size,
        /// which is the case when it is read-only.
        /// </summary>
        bool IDictionary.IsFixedSize
        {
            get { return isReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        bool IDictionary.IsReadOnly
        {
            get { return isReadOnly; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the dictionary contains an element
        /// with the specified key.
        /// </summary>
        /// <param name="key">
        /// The key to locate.
        /// </param>
        /// <returns>
        /// True if the dictionary contains an element with the specified key;
        /// otherwise, false.
        /// </returns>
        bool IDictionary.Contains(
            object key /* in */
            )
        {
            if (IsCompatibleKey(key))
                return ContainsKey((TKey)key);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified key and value to the dictionary,
        /// throwing an exception if the key already exists.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
        void IDictionary.Add(
            object key,  /* in */
            object value /* in */
            )
        {
            Add((TKey)key, (TValue)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the element with the specified key from the
        /// dictionary.
        /// </summary>
        /// <param name="key">
        /// The key of the element to remove.
        /// </param>
        void IDictionary.Remove(
            object key /* in */
            )
        {
            if (IsCompatibleKey(key))
                Remove((TKey)key);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a dictionary enumerator that iterates over a
        /// snapshot of the key/value pairs in the dictionary.
        /// </summary>
        /// <returns>
        /// A dictionary enumerator over a snapshot of the key/value pairs in
        /// the dictionary.
        /// </returns>
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
        /// <summary>
        /// Gets a value indicating whether access to the dictionary is
        /// synchronized (thread-safe).
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets an object that can be used to synchronize access to the
        /// dictionary.
        /// </summary>
        object ICollection.SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies a snapshot of the dictionary's entries, as
        /// dictionary entries, to the specified array starting at the specified
        /// index.
        /// </summary>
        /// <param name="array">
        /// The destination array that receives the dictionary entries.  This
        /// parameter may not be null.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in the destination array at which copying
        /// begins.
        /// </param>
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
        /// <summary>
        /// This class represents the collection of keys returned by the
        /// dictionary.  It is a list of keys that also implements the Eagle
        /// collection interface.
        /// </summary>
        [ObjectId("950a2f1c-faaa-40da-a58b-0fbfe07f1b0c")]
        public sealed class FastKeyCollection :
                List<TKey>, IAnyCollection<TKey>
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a key collection containing the keys from the
            /// specified sequence.
            /// </summary>
            /// <param name="collection">
            /// The sequence of keys to copy into the new collection.
            /// </param>
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
        /// <summary>
        /// This class represents the collection of values returned by the
        /// dictionary.  It is a list of values that also implements the Eagle
        /// collection interface.
        /// </summary>
        [ObjectId("79dd8044-55c7-466d-aa8e-294a120c6cda")]
        public sealed class FastValueCollection :
                List<TValue>, IAnyCollection<TValue>
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a value collection containing the values from the
            /// specified sequence.
            /// </summary>
            /// <param name="collection">
            /// The sequence of values to copy into the new collection.
            /// </param>
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
        /// <summary>
        /// This class represents a snapshot-based dictionary enumerator for the
        /// non-generic dictionary enumeration of the containing dictionary.
        /// </summary>
        [ObjectId("1e1f9f28-4593-45bd-bbc7-f86df4babc2b")]
        private sealed class FastDictionaryEnumerator : IDictionaryEnumerator
        {
            /// <summary>
            /// The snapshot of key/value pairs over which this enumerator
            /// iterates.
            /// </summary>
            private readonly KeyValuePair<TKey, TValue>[] snapshot;

            /// <summary>
            /// The current zero-based position within the snapshot, or
            /// negative one when the enumerator is positioned before the first
            /// element.
            /// </summary>
            private int position;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Constructs a dictionary enumerator over the specified snapshot
            /// of key/value pairs.
            /// </summary>
            /// <param name="snapshot">
            /// The snapshot of key/value pairs to enumerate.
            /// </param>
            internal FastDictionaryEnumerator(
                KeyValuePair<TKey, TValue>[] snapshot /* in */
                )
            {
                this.snapshot = snapshot;
                this.position = -1;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets the dictionary entry at the current position of the
            /// enumerator.
            /// </summary>
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

            /// <summary>
            /// Gets the key of the dictionary entry at the current position of
            /// the enumerator.
            /// </summary>
            public object Key
            {
                get { return snapshot[position].Key; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets the value of the dictionary entry at the current position
            /// of the enumerator.
            /// </summary>
            public object Value
            {
                get { return snapshot[position].Value; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public object Current
            {
                get { return Entry; }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method advances the enumerator to the next element of the
            /// snapshot.
            /// </summary>
            /// <returns>
            /// True if the enumerator was successfully advanced to the next
            /// element; false if the enumerator has passed the end of the
            /// snapshot.
            /// </returns>
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

            /// <summary>
            /// This method resets the enumerator to its initial position,
            /// which is before the first element of the snapshot.
            /// </summary>
            public void Reset()
            {
                position = -1;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the equality comparer used for key comparisons and hash code
        /// computation.
        /// </summary>
        public virtual IEqualityComparer<TKey> Comparer
        {
            get { return comparer; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method attempts to add the specified key and value to the
        /// dictionary, doing nothing if the key already exists.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.  This parameter may not be null.
        /// </param>
        /// <param name="value">
        /// The value of the element to add.
        /// </param>
        /// <returns>
        /// True if the key and value were added; false if the key already
        /// exists.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the dictionary contains the
        /// specified value.
        /// </summary>
        /// <param name="value">
        /// The value to locate.
        /// </param>
        /// <returns>
        /// True if the dictionary contains the specified value; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method returns an array containing the keys of all live
        /// entries in the dictionary.
        /// </summary>
        /// <returns>
        /// An array containing the keys of all live entries in the dictionary.
        /// </returns>
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

        /// <summary>
        /// This method returns an array containing the values of all live
        /// entries in the dictionary.
        /// </summary>
        /// <returns>
        /// An array containing the values of all live entries in the
        /// dictionary.
        /// </returns>
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

        /// <summary>
        /// This method returns a new key collection containing the keys of all
        /// live entries in the dictionary.
        /// </summary>
        /// <returns>
        /// A new key collection containing the keys of all live entries in the
        /// dictionary.
        /// </returns>
        private IAnyCollection<TKey> InternalGetKeyCollection()
        {
            return new FastKeyCollection(InternalGetKeys());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a new value collection containing the values of
        /// all live entries in the dictionary.
        /// </summary>
        /// <returns>
        /// A new value collection containing the values of all live entries in
        /// the dictionary.
        /// </returns>
        private IAnyCollection<TValue> InternalGetValueCollection()
        {
            return new FastValueCollection(InternalGetValues());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the dictionary contains the
        /// specified value without acquiring the synchronization lock, using
        /// the default equality comparer for the value type.
        /// </summary>
        /// <param name="value">
        /// The value to locate.
        /// </param>
        /// <returns>
        /// True if the dictionary contains the specified value; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method populates the specified serialization information with
        /// the data needed to serialize the dictionary, including its
        /// comparer, keys, values, and read-only state.
        /// </summary>
        /// <param name="info">
        /// The serialization information to populate with the serialized state
        /// of the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// This method returns a string representation of the dictionary,
        /// consisting of the keys of all live entries separated by spaces.
        /// </summary>
        /// <returns>
        /// A string containing the keys of all live entries in the dictionary
        /// separated by spaces, or an empty string if the dictionary is empty.
        /// </returns>
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
