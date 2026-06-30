/*
 * DateTimeListDictionary.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.Collections.Generic.List<System.DateTime>>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.Collections.Generic.List<System.DateTime>>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to
    /// chronologically sorted lists of <see cref="DateTime" /> values.  It is
    /// typically used to track timestamped events per key; it supports inserting
    /// values in sorted order, counting values that fall within a time window,
    /// and compacting away values that are older than a given epoch.  The
    /// inherited mutating members are deliberately disabled in favor of the
    /// specialized members provided here.
    /// </summary>
    [ObjectId("5abc22da-28bb-4d04-9c9d-c1067aa50c4f")]
    internal sealed class DateTimeListDictionary :
            SomeDictionary, IDictionary<string, List<DateTime>>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public DateTimeListDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method removes, from the specified sorted list, every value that
        /// occurs strictly before the specified epoch.
        /// </summary>
        /// <param name="value">
        /// The chronologically sorted list of values to compact.
        /// </param>
        /// <param name="epoch">
        /// The cutoff time; values that occur before this time are removed.
        /// </param>
        /// <returns>
        /// The number of values that were removed from the list, or an invalid
        /// count if the operation could not be performed.
        /// </returns>
        private int Compact(
            List<DateTime> value,
            DateTime epoch
            )
        {
            if (value == null)
            {
                //
                // NOTE: This is impossible to hit because all
                //       callers check for a null value prior
                //       to calling this method.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            int count = value.Count;

            if (count == 0)
            {
                //
                // NOTE: This is impossible to hit because the
                //       list of DateTime values is created with
                //       at least one element -AND- zero element
                //       lists are removed by the caller to this
                //       method.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            int index = value.BinarySearch(epoch);

            if (index < 0)
                index = ~index;

            if (index > count)
            {
                //
                // NOTE: This is impossible to hit because the
                //       BinarySearch method does not return a
                //       positive value greater then the final
                //       index in the list.
                //
                return _Constants.Count.Invalid; /* IMPOSSIBLE */
            }

            value.RemoveRange(0, index);
            return index;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compacts every list in this dictionary by removing the
        /// values that occur before the specified epoch, and removes any key
        /// whose list becomes empty (or is null) as a result.
        /// </summary>
        /// <param name="epoch">
        /// The cutoff time; values that occur before this time are removed.
        /// </param>
        /// <returns>
        /// The total number of values that were removed across all keys.
        /// </returns>
        private int Compact(
            DateTime epoch
            )
        {
            int count = 0;
            StringList keys = new StringList(base.Keys);

            foreach (string key in keys)
            {
                if (key == null)
                    continue;

                List<DateTime> value;

                if (!base.TryGetValue(key, out value))
                    continue;

                if (value == null)
                {
                    base.Remove(key);
                    continue;
                }

                int valueCount = Compact(value, epoch);

                if (valueCount > 0)
                {
                    count += valueCount;

                    if (value.Count == 0)
                        base.Remove(key);
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDictionary<string, IntArgumentPair> Overrides
        /// <summary>
        /// Gets the list of values associated with the specified key; setting
        /// the list for a key is not supported and always throws a
        /// <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="key">
        /// The key whose associated list of values is to be retrieved.
        /// </param>
        /// <returns>
        /// The list of values associated with the specified key.
        /// </returns>
        List<DateTime> IDictionary<string, List<DateTime>>.this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws a
        /// <see cref="NotSupportedException" />.  Values must be added using the
        /// specialized add method that maintains chronological order.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The list of values to associate with the key.
        /// </param>
        void IDictionary<string, List<DateTime>>.Add(
            string key,
            List<DateTime> value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Explicit IDictionary<string, List<DateTime>> Overrides
        /// <summary>
        /// Gets the list of values associated with the specified key; setting
        /// the list for a key is not supported and always throws a
        /// <see cref="NotSupportedException" />.
        /// </summary>
        /// <param name="key">
        /// The key whose associated list of values is to be retrieved.
        /// </param>
        /// <returns>
        /// The list of values associated with the specified key.
        /// </returns>
        public new List<DateTime> this[string key]
        {
            get { return base[key]; }
            set { throw new NotSupportedException(); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is not supported and always throws a
        /// <see cref="NotSupportedException" />.  Values must be added using the
        /// specialized add method that maintains chronological order.
        /// </summary>
        /// <param name="key">
        /// The key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The list of values to associate with the key.
        /// </param>
        public new void Add(
            string key,
            List<DateTime> value
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Gets the current coordinated universal time (UTC) used by this
        /// dictionary as the reference point for time-window calculations.
        /// </summary>
        public DateTime Now
        {
            get { return TimeOps.GetUtcNow(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method counts the values associated with the specified key that
        /// occur within the most recent interval of the specified length, as
        /// measured backward from the current time.
        /// </summary>
        /// <param name="key">
        /// The key whose recent values are to be counted.
        /// </param>
        /// <param name="timeSpan">
        /// The length of the trailing time window to consider.
        /// </param>
        /// <returns>
        /// The number of values associated with the key that fall within the
        /// time window.
        /// </returns>
        public int CountFrom(
            string key,
            TimeSpan timeSpan
            )
        {
            return CountFrom(key, Now.Subtract(timeSpan));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the values associated with the specified key that
        /// occur at or after the specified epoch.
        /// </summary>
        /// <param name="key">
        /// The key whose values are to be counted.
        /// </param>
        /// <param name="epoch">
        /// The cutoff time; only values at or after this time are counted.  If
        /// this is null, all values associated with the key are counted.
        /// </param>
        /// <returns>
        /// The number of values associated with the key that occur at or after
        /// the epoch.
        /// </returns>
        public int CountFrom(
            string key,
            DateTime? epoch
            )
        {
            List<DateTime> list;

            if (!base.TryGetValue(key, out list))
                return 0;

            int count = list.Count;

            if (epoch == null)
                return count;

            int index = list.BinarySearch((DateTime)epoch);

            if (index < 0)
                index = ~index;

            return count - index;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified value to the chronologically sorted
        /// list associated with the specified key, creating the list if it does
        /// not already exist.  If an epoch is supplied, the entire dictionary is
        /// first compacted to remove values older than that epoch.
        /// </summary>
        /// <param name="key">
        /// The key to associate the value with.
        /// </param>
        /// <param name="value">
        /// The value to insert, in sorted order, into the list for the key.
        /// </param>
        /// <param name="epoch">
        /// The cutoff time used to compact the dictionary before adding.  If this
        /// is null, no compaction is performed.
        /// </param>
        public void Add(
            string key,
            DateTime value,
            DateTime? epoch
            )
        {
            if (epoch != null)
                Compact((DateTime)epoch);

            List<DateTime> list;

            if (base.TryGetValue(key, out list))
            {
                int index = list.BinarySearch(value);

                if (index < 0)
                    index = ~index;

                list.Insert(index, value);
            }
            else
            {
                list = new List<DateTime>();
                list.Add(value);

                base.Add(key, list);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this dictionary to a string in the Eagle list format, where
        /// each matching key is followed by the list of its values formatted for
        /// tracing.  Keys may optionally be restricted to those matching the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order for its entry to be
        /// included in the resulting string.  This parameter may be null, in
        /// which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList result = new StringList();
            StringList keys = new StringList(this.Keys);

            foreach (string key in keys)
            {
                if (key == null)
                    continue;

                if ((pattern != null) && !Parser.StringMatch(
                        null, key, 0, pattern, 0, noCase))
                {
                    continue;
                }

                result.Add(key);

                List<DateTime> values;

                if (!base.TryGetValue(key, out values))
                    continue;

                if (values == null)
                    continue;

                StringList subResult = new StringList();

                foreach (DateTime value in values)
                {
                    subResult.Add(
                        FormatOps.TraceDateTime(value, false));
                }

                result.Add(subResult.ToString());
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this dictionary to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
